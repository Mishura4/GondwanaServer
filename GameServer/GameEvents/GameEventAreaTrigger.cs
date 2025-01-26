using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.Language;
using DOLDatabase.Tables;
using System.Security.Cryptography;
using DOL.GS.PacketHandler;
using log4net;
using System.Reflection;

namespace DOL.GameEvents
{
    public class GameEventAreaTrigger
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);
        private object _db;
        public List<GamePlayer> PlayersInArea { get; } = new List<GamePlayer>();
        public List<GamePlayer> PlayersUsedItem { get; }
        public List<GamePlayer> PlayersWhispered { get; }
        public Timer LaunchTimer { get; }
        public Timer MobCheckTimer { get; }
        public AbstractArea Area { get; set; }
        public GameEvent Event { get; set; }

        private bool m_hasMobs = true;
        public bool AllowItemDestroy { get; set; }
        public int UseItemEffect { get; set; }
        public int UseItemSound { get; set; }

        public bool IsPlayerEnterAreaCondition => PlayersUsedItem == null && PlayersWhispered == null;

        public GameEventAreaTrigger(GameEvent ev, AreaXEvent db, AbstractArea area)
        {
            _db = db.Clone();
            Event = ev;
            this.LaunchTimer = new Timer();
            this.MobCheckTimer = new Timer();
            if (!string.IsNullOrEmpty(db.UseItem))
            {
                PlayersUsedItem = new List<GamePlayer>();
            }
            if (!string.IsNullOrEmpty(db.Whisper))
            {
                PlayersWhispered = new List<GamePlayer>();
            }
            Mobs = new();
            Area = area;

            ParseValuesFromDb(db);
            Init();

            if (RequiredPlayerCount > 1 && ev.IsInstancedEvent && ev.InstancedConditionType == InstancedConditionTypes.Player)
            {
                log.Warn($"Event {ev.EventName} ({ev.ID}) requires {RequiredPlayerCount} players, but it is instanced by player, there will never be more than 1!");
            }
        }

        public GameEventAreaTrigger(GameEventAreaTrigger areaTrigger)
        {
            _db = areaTrigger._db;
            this.LaunchTimer = new Timer();
            this.MobCheckTimer = new Timer();
            PlayersUsedItem = areaTrigger.PlayersUsedItem == null ? null : new List<GamePlayer>(areaTrigger.PlayersUsedItem);
            PlayersWhispered = areaTrigger.PlayersWhispered == null ? null : new List<GamePlayer>(areaTrigger.PlayersWhispered);
            AreaID = areaTrigger.AreaID;
            Area = areaTrigger.Area;

            ParseValuesFromDb((AreaXEvent)_db);
            Init();
        }

        public void Init()
        {
            if (TimerCount > 0)
            {
                LaunchTimer.Interval = ((AreaXEvent)_db).TimerCount * 1000;
                LaunchTimer.Elapsed += LaunchTimer_Elapsed;
            }
            m_hasMobs = Mobs?.Count is null or 0;
            if (!m_hasMobs)
            {
                MobCheckTimer.Interval = 2000; // update every 2 seconds
                MobCheckTimer.Elapsed += MobCheckTimer_Elapsed;
                MobCheckTimer.AutoReset = true;
                MobCheckTimer.Start();
            }
        }

        public void ParseValuesFromDb(AreaXEvent db)
        {
            EventID = db.EventID;
            AreaID = db.AreaID;
            RequiredPlayerCount = db.PlayersNb;
            Mobs = new();
            if (!string.IsNullOrEmpty(db.Mobs))
            {
                var mobs = db.Mobs.Split(new char[] { ';' });
                foreach (var mob in mobs)
                {
                    var mobInfo = mob.Split(new char[] { '|' });
                    Mobs.Add(mobInfo[0], int.Parse(mobInfo[1]));
                }
            }
            UseItem = !string.IsNullOrEmpty(db.UseItem) ? db.UseItem : null;
            Whisper = !string.IsNullOrEmpty(db.Whisper) ? db.Whisper : null;
            PlayersLeave = db.PlayersLeave;
            ResetEvent = db.ResetEvent;
            TimerCount = db.TimerCount;
            AllowItemDestroy = db.AllowItemDestroy;
            UseItemEffect = db.UseItemEffect;
            UseItemSound = db.UseItemSound;
        }

        private void LaunchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            LaunchTimer.Stop();
            Reset();

            foreach (var cl in WorldMgr.GetAllPlayingClients().Where(c => c.Player.CurrentAreas.Any(
                    a => ((AbstractArea)a).DbArea != null && AreaID == (((AbstractArea)a).DbArea.ObjectId))))
            {
                ChatUtil.SendImportant(cl.Player, LanguageMgr.GetTranslation(cl.Account.Language, "Area.Event.Timer.Stop"));
            }

            if (ResetEvent == true)
            {
                Event.Reset();
            }
            else
            {
                Task.Run(() => Event.Stop(EndingConditionType.Timer));
            }
        }

        private void PlayerUseItemEvent(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is not GamePlayer player || args is not UseSlotEventArgs useArgs)
                return;
            
            if (!player.CurrentAreas.Contains(Area))
            {
                log.Warn($"GameEventAreaTrigger ${Event} detected player ${player.Name} used an item, but player is not in area {Area.Description}");
                return;
            }
            if (useArgs.Item?.Id_nb == UseItem)
            {
                if (Event.IsRunning)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Area.Event.CannotUseItemAgain"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                lock (PlayersUsedItem)
                {
                    if (PlayersUsedItem.Contains(player))
                        return;
                }

                if (AllowItemDestroy)
                {
                    player.TempProperties.setProperty("AreaTriggerForDestroy", this);
                    player.TempProperties.setProperty("UseItemSlot", useArgs.Slot);
                    player.Out.SendCustomDialog(LanguageMgr.GetTranslation(player.Client.Account.Language, "Area.Event.ConfirmUseItem", useArgs.Item.Name), new CustomDialogResponse(UseItemDestroyConfirmation));
                }
                else
                {
                    lock (PlayersUsedItem)
                    {
                        PlayersUsedItem.Add(player);
                    }

                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Area.Event.UsedItem", useArgs.Item.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    TryStartEvent();
                }
            }
        }

        private static void UseItemDestroyConfirmation(GamePlayer player, byte response)
        {
            if (response != 0x01)
                return;

            var areaTrigger = player.TempProperties.getProperty<GameEventAreaTrigger>("AreaTriggerForDestroy", null);
            var slot = player.TempProperties.getProperty<int>("UseItemSlot", -1);

            player.TempProperties.removeProperty("AreaTriggerForDestroy");
            player.TempProperties.removeProperty("UseItemSlot");

            if (areaTrigger == null || slot < 0)
                return;

            var item = player.Inventory.GetItem((eInventorySlot)slot);
            if (item == null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Area.Event.ItemNotInBackpack"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            player.Inventory.RemoveCountFromStack(item, 1);

            lock (areaTrigger.PlayersUsedItem)
            {
                if (!areaTrigger.PlayersUsedItem.Contains(player))
                    areaTrigger.PlayersUsedItem.Add(player);
            }

            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Area.Event.UsedItem", item.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);

            if (areaTrigger.UseItemEffect > 0)
            {
                player.Out.SendSpellEffectAnimation(player, player, (ushort)areaTrigger.UseItemEffect, 0, false, 1);
            }

            if (areaTrigger.UseItemSound > 0)
            {
                player.Out.SendSoundEffect((ushort)areaTrigger.UseItemSound, player.Position, 0);
            }

            areaTrigger.TryStartEvent();
        }

        private void PlayerWhisperEvent(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is not GamePlayer player)
                return;

            if (!player.CurrentAreas.Contains(Area))
            {
                log.Warn($"GameEventAreaTrigger ${Event} detected player ${player.Name} whispered, but player is not in area {Area.Description}");
                return;
            }
            string text = string.Empty;
            if (args is WhisperEventArgs whisper)
            {
                text = whisper.Text;
            }
            else if (args is SayEventArgs say)
            {
                text = say.Text;
            }
            if (text != null && text.Contains(Whisper, StringComparison.InvariantCultureIgnoreCase))
            {
                lock (PlayersWhispered)
                {
                    if (!PlayersWhispered.Contains(player))
                        PlayersWhispered.Add(player);
                }
                TryStartEvent();
            }
        }

        public void PlayerEntersArea(GamePlayer player, IArea area)
        {
            if (area != Area)
                return;

            lock (PlayersInArea)
            {
                if (!PlayersInArea.Contains(player))
                {
                    PlayersInArea.Add(player);
                    if (PlayersUsedItem != null)
                    {
                        GameEventMgr.AddHandler(player, GamePlayerEvent.UseSlot, PlayerUseItemEvent);
                    }
                    if (PlayersWhispered != null)
                    {
                        GameEventMgr.AddHandler(player, GamePlayerEvent.Whisper, PlayerWhisperEvent);
                        GameEventMgr.AddHandler(player, GamePlayerEvent.Say, PlayerWhisperEvent);
                    }
                }
            }
            if (IsPlayerEnterAreaCondition) // Only start timer on player entering if the condition is that players enter, and not give item or whisper
                TryStartEvent();
        }

        public void PlayerLeavesArea(GamePlayer player, IArea area)
        {
            if (area != Area)
                return;

            int players;
            lock (PlayersInArea)
            {
                if (!PlayersInArea.Remove(player))
                    return;
                players = PlayersInArea.Count;
            }

            if (Event == null)
                return;

            // TODO: Clean up instances upstream?
            if (PlayersLeave == true && players == 0)
            {
                bool isSinglePlayerInstance = (Event.InstancedConditionType == InstancedConditionTypes.Player);
                string cancelMessage = isSinglePlayerInstance
                    ? LanguageMgr.GetTranslation(player.Client.Account.Language, "Area.Event.LeftAreaCancelSingle")
                    : LanguageMgr.GetTranslation(player.Client.Account.Language, "Area.Event.LeftAreaCancelMultiple");

                ChatUtil.SendImportant(player, cancelMessage);

                LaunchTimer.Stop();
                if (ResetEvent == true)
                    Event.Reset();
                else
                    Task.Run(() => Event.Stop(EndingConditionType.AreaEvent));
            }
            if (PlayersUsedItem != null)
            {
                GameEventMgr.RemoveHandler(player, GamePlayerEvent.UseSlot, PlayerUseItemEvent);
            }
            if (PlayersWhispered != null)
            {
                GameEventMgr.RemoveHandler(player, GamePlayerEvent.Say, PlayerWhisperEvent);
                GameEventMgr.RemoveHandler(player, GamePlayerEvent.Whisper, PlayerWhisperEvent);
            }
        }

        private bool CheckMobs()
        {
            var counts = new Dictionary<string, int>(Mobs.Keys.Select(k => (new KeyValuePair<string, int>(k, 0))));
            foreach (var mob in Area.ZoneIn.GetNPCsOfZone(eRealm.None).Where(c => Area.IsContaining(c.Position.Coordinate))) // Surely there's a better way than checking every mob in the entire zone?
            {
                if (counts.TryGetValue(mob.Name, out var value))
                {
                    counts[mob.Name] = value + 1;
                }
            }
            if (Mobs.Any(required => counts.GetValueOrDefault(required.Key, 0) < required.Value))
            {
                string s = String.Join(',', Mobs.Select(pair => pair.Key + ": " + pair.Value));
                m_hasMobs = false;
                return false;
            }
            m_hasMobs = true;
            TryStartEvent();
            return true;
        }

        private void MobCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!CheckMobs())
                return;
            
            TryStartEvent();
        }

        public bool TryStartEvent()
        {
            if (!Event.IsReady)
                return false;

            if (!CheckConditions())
            {
                if (TimerCount > 0)
                {
                    LaunchTimer.Interval = TimerCount * 1000;
                    LaunchTimer.Start();

                    foreach (var player in PlayersInArea)
                    {
                        ChatUtil.SendImportant(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "Area.Event.Timer.Start", TimerCount));
                    }
                }
                return false;
            }
            else
            {
                if (LaunchTimer.Enabled)
                    LaunchTimer.Stop();

                Task.Run(() => Event.Start(Event.Owner));
            }
            return true;
        }

        public bool CheckConditions(AbstractArea area = null)
        {
            if (PlayersUsedItem == null && PlayersWhispered == null) // Count is about the current players in the area
            {
                if (PlayersInArea.Count < RequiredPlayerCount)
                    return false;
            }
            else
            {
                if (PlayersUsedItem != null && PlayersUsedItem.Count < RequiredPlayerCount)
                    return false;
                if (PlayersWhispered != null && PlayersWhispered.Count != RequiredPlayerCount)
                    return false;
            }
            if (!m_hasMobs)
                return false;
            return true;
        }
        
        public void Reset()
        {
            LaunchTimer.Stop();
            PlayersUsedItem?.Clear();
            PlayersWhispered?.Clear();
        }

        public string EventID
        {
            get;
            set;
        }

        public string AreaID
        {
            get;
            set;
        }


        public int? RequiredPlayerCount
        {
            get;
            set;
        }

        public Dictionary<string, int> Mobs
        {
            get;
            set;
        }

        public string UseItem
        {
            get;
            set;
        }

        public string Whisper
        {
            get;
            set;
        }

        public bool? PlayersLeave
        {
            get;
            set;
        }

        public bool? ResetEvent
        {
            get;
            set;
        }

        public int TimerCount
        {
            get;
            set;
        }
    }
}