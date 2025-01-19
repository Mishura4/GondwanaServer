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

namespace DOL.GameEvents
{
    public class GameEventAreaTrigger
    {
        private object _db;
        public List<GamePlayer> PlayersInArea { get; } = new List<GamePlayer>();
        public List<GamePlayer> PlayersUsedItem { get; }
        public List<GamePlayer> PlayersWhispered { get; }
        public Timer LaunchTimer { get; }
        public Timer MobCheckTimer { get; }
        public AbstractArea Area { get; set; }
        public GameEvent MasterEvent { get; set; }

        private bool m_hasMobs = true;

        public GameEventAreaTrigger(GameEvent masterEvent, AreaXEvent db, AbstractArea area)
        {
            _db = db.Clone();
            MasterEvent = masterEvent;
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

            LaunchTimer.Interval = db.TimerCount * 1000;
            LaunchTimer.Elapsed += LaunchTimer_Elapsed;
            MobCheckTimer.Interval = 2000; // update every 2 seconds
            MobCheckTimer.Elapsed += MobCheckTimer_Elapsed;
            if (Mobs != null && Mobs.Count != 0)
            {
                MobCheckTimer.AutoReset = true;
                MobCheckTimer.Start();
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

            LaunchTimer.Interval = ((AreaXEvent)_db).TimerCount * 1000;
            LaunchTimer.Elapsed += LaunchTimer_Elapsed;

            if (Mobs.Count > 0)
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
        }

        private void LaunchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Reset();

            foreach (var cl in WorldMgr.GetAllPlayingClients().Where(c => c.Player.CurrentAreas.Any(
                    a => ((AbstractArea)a).DbArea != null && AreaID == (((AbstractArea)a).DbArea.ObjectId))))
            {
                ChatUtil.SendImportant(cl.Player, LanguageMgr.GetTranslation(cl.Account.Language, "Area.Event.Timer.Stop"));
            }
        }

        private void PlayerUseItemEvent(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is not GamePlayer player || args is not UseSlotEventArgs useArgs)
                return;

            if (useArgs.Item?.Id_nb == UseItem)
            {
                lock (PlayersUsedItem)
                {
                    if (!PlayersUsedItem.Contains(player))
                        PlayersUsedItem.Add(player);
                }
            }
        }

        private void PlayerWhisperEvent(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is not GamePlayer player || args is not WhisperEventArgs whisperArgs)
                return;

            if (String.Equals(whisperArgs.Text, Whisper, StringComparison.InvariantCultureIgnoreCase))
            {
                lock (PlayersWhispered)
                {
                    if (!PlayersWhispered.Contains(player))
                        PlayersWhispered.Add(player);
                }
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
                    }
                }
            }
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

            if (MasterEvent == null)
                return;

            // TODO: Clean up instances upstream?
            if (PlayersLeave == true && players == 0)
            {
                LaunchTimer.Stop();
                if (ResetEvent == true)
                    MasterEvent.Reset();
                else
                    Task.Run(() => MasterEvent.Stop(EndingConditionType.AreaEvent));
            }
            if (PlayersUsedItem != null)
            {
                GameEventMgr.RemoveHandler(player, GamePlayerEvent.UseSlot, PlayerUseItemEvent);
            }
            if (PlayersWhispered != null)
            {
                GameEventMgr.RemoveHandler(player, GamePlayerEvent.Whisper, PlayerWhisperEvent);
            }
            TryStartEvent();
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
            if (!MasterEvent.IsReady)
                return false;

            if (!CheckConditions())
            {
                if (TimerCount > 0 && !LaunchTimer.Enabled)
                {
                    LaunchTimer.Start();

                    foreach (var player in PlayersInArea)
                    {
                        ChatUtil.SendImportant(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "Area.Event.Timer.Start", TimerCount));
                    }
                }
                return false;
            }
            
            LaunchTimer.Stop();
            Task.Run(() => MasterEvent.Start(MasterEvent.Owner));
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