using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using DOL.Database;
using DOL.GS;
using DOL.Language;
using DOLDatabase.Tables;

namespace DOL.GameEvents
{
    public class AreaGameEvent
    {
        private object _db;
        public int PlayersCounter { get; set; }
        public int UseItemCounter { get; set; }
        public int WhisperCounter { get; set; }
        public Timer LaunchTimer { get; }
        public Timer MobCheckTimer { get; }
        public AbstractArea Area { get; set; }

        public AreaGameEvent(AreaXEvent db)
        {
            _db = db.Clone();
            this.LaunchTimer = new Timer();
            this.MobCheckTimer = new Timer();
            PlayersCounter = 0;
            UseItemCounter = 0;
            WhisperCounter = 0;
            Mobs = new Dictionary<string, int>();

            ParseValuesFromDb(db);

            LaunchTimer.Interval = db.TimerCount * 1000;
            LaunchTimer.Elapsed += LaunchTimer_Elapsed;
            MobCheckTimer.Interval = 2000; // update every 2 seconds
            MobCheckTimer.Elapsed += MobCheckTimer_Elapsed;
            if (Mobs.Count() != 0 && Mobs != null)
            {
                MobCheckTimer.AutoReset = true;
                MobCheckTimer.Start();
            }
        }

        public void ParseValuesFromDb(AreaXEvent db)
        {
            EventID = db.EventID;
            AreaID = db.AreaID;
            PlayersNb = db.PlayersNb;
            if (db.Mobs != "" && db.Mobs != null)
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
            LaunchTimer.Stop();
            PlayersCounter = 0;
            UseItemCounter = 0;
            WhisperCounter = 0;

            var areaEvent = GetGameEvent();

            foreach (var cl in WorldMgr.GetAllPlayingClients().Where(c => c.Player.CurrentAreas.Any(
                    a => ((AbstractArea)a).DbArea != null && AreaID == (((AbstractArea)a).DbArea.ObjectId))))
            {
                ChatUtil.SendImportant(cl.Player, LanguageMgr.GetTranslation(cl.Account.Language, "Area.Event.Timer.Stop"));
            }
        }

        private void MobCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //check if all the mobs are in the area
            var areaEvent = GetGameEvent();
            if (areaEvent != null)
            {
                foreach (var mob in Mobs)
                {
                    var mobCount = WorldMgr.GetNPCsByName(mob.Key, eRealm.None).Where(c => Area.IsContaining(c.Position.X, c.Position.Y, c.Position.Z)).ToList().Count();

                    if (mobCount < mob.Value)
                    {
                        return;
                    }
                }
                Task.Run(() => GameEventManager.Instance.StartEvent(areaEvent, this));
                MobCheckTimer.Stop();
            }
        }

        public async Task ResetAreaEvent()
        {
            if (PlayersLeave)
            {
                var areaEvent = GameEventManager.Instance.Events.FirstOrDefault(e =>
                    e.AreaStartingId?.Equals(AreaID) == true &&
                    e.StartedTime.HasValue &&
                    e.StartConditionType == StartingConditionType.Areaxevent);
                if (areaEvent != null)
                {
                    PlayersCounter = GetPlayersInArea().Count();
                    if (PlayersCounter == 0)
                    {
                        LaunchTimer.Stop();
                        if (ResetEvent)
                            GameEventManager.Instance.ResetEvent(areaEvent);
                        else
                            await GameEventManager.Instance.StopEvent(areaEvent, EndingConditionType.AreaEvent);
                    }
                }
            }
        }
        public bool CheckConditions(AbstractArea area = null)
        {
            if (area != null)
                Area = area;
            if (Area == null)
                return false;

            var areaEvent = GetGameEvent();

            if (areaEvent != null)
            {
                if (Mobs.Count() != 0 && Mobs != null && !MobCheckTimer.Enabled)
                {
                    MobCheckTimer.Start();
                }
                //recheck number of players in area
                PlayersCounter = GetPlayersInArea().Count();

                if (PlayersNb == PlayersCounter)
                {
                    if ((UseItem != null && Whisper == null && UseItemCounter == PlayersNb) ||
                        (Whisper != null && UseItem == null && WhisperCounter == PlayersNb) ||
                        (UseItem != null && Whisper != null && UseItemCounter == PlayersNb && WhisperCounter == PlayersNb) ||
                        (UseItem == null && Whisper == null))
                    {
                        Task.Run(() => GameEventManager.Instance.StartEvent(areaEvent, this));
                        LaunchTimer.Stop();
                        return true;
                    }
                }
                if (!LaunchTimer.Enabled && (UseItem != null && UseItemCounter != 0) ||
                        (Whisper != null && WhisperCounter != 0) ||
                        (UseItem == null && Whisper == null))
                {
                    LaunchTimer.Start();

                    foreach (var cl in GetPlayersInArea())
                    {
                        ChatUtil.SendImportant(cl.Player, LanguageMgr.GetTranslation(cl.Account.Language, "Area.Event.Timer.Start", TimerCount));
                    }
                }
            }
            return false;
        }

        public List<GameClient> GetPlayersInArea()
        {
            return WorldMgr.GetAllPlayingClients().Where(c => Area.IsContaining(c.Player.Position.X, c.Player.Position.Y, c.Player.Position.Z)).ToList();
        }

        GameEvent GetGameEvent()
        {
            return GameEventManager.Instance.Events.FirstOrDefault(e =>
            e.AreaStartingId?.Equals(AreaID) == true &&
           !e.StartedTime.HasValue &&
            e.Status == EventStatus.NotOver &&
            e.StartConditionType == StartingConditionType.Areaxevent);
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


        public int PlayersNb
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

        public bool PlayersLeave
        {
            get;
            set;
        }

        public bool ResetEvent
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