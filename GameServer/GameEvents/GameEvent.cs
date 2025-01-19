using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOLDatabase.Tables;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;
using DOL.Language;
using Grpc.Core;
using System.Net;
using DOL.MobGroups;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace DOL.GameEvents
{
    public class GameEvent
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);
        private object _db;
        private GamePlayer owner;

        public Timer RandomTextTimer { get; }
        public Timer RemainingTimeTimer { get; }

        public Timer ResetFamilyTimer { get; }

        public Dictionary<string, ushort> StartEffects;
        public Dictionary<string, ushort> EndEffects;
        public Dictionary<string, GameNPC> RemovedMobs { get; }
        public Dictionary<string, GameStaticItem> RemovedCoffres { get; }
        public List<GameNPC> RelatedNPCs { get; } = new();
        public GameEventAreaTrigger? AreaConditions { get; init; }

        /**
         * @brief Event instance constructor
         */
        public GameEvent(GameEvent ev)
        {
            ParallelLaunch = ev.ParallelLaunch;
            IsInstanceMaster = false;
            _db = ev._db;
            ID = ev.ID;
            RandomTextTimer = new Timer();
            RemainingTimeTimer = new Timer();
            ResetFamilyTimer = new Timer();
            Owner = ev.Owner;

            EventAreas = ev.EventAreas;
            EventChance = ev.EventChance;
            EventName = ev.EventName;
            EventZones = ev.EventZones;
            ShowEvent = ev.ShowEvent;
            StartConditionType = ev.StartConditionType;
            EventChanceInterval = ev.EventChanceInterval;
            DebutText = ev.DebutText;
            EndText = ev.EndText;
            StartedTime = (DateTimeOffset?)null;
            EndingConditionTypes = ev.EndingConditionTypes;
            RandomText = ev.RandomText;
            RandTextInterval = ev.RandTextInterval;
            RemainingTimeInterval = ev.RemainingTimeInterval;
            RemainingTimeText = ev.RemainingTimeText;
            EndingActionA = ev.EndingActionA;
            EndingActionB = ev.EndingActionB;
            MobNamesToKill = ev.MobNamesToKill;
            EndActionStartEventID = ev.EndActionStartEventID;
            StartActionStopEventID = ev.StartActionStopEventID;
            StartTriggerTime = ev.StartTriggerTime;
            TimerType = ev.TimerType;
            EndTime = (DateTimeOffset?)null;
            ChronoTime = ev.ChronoTime;
            KillStartingGroupMobId = ev.KillStartingGroupMobId;
            ResetEventId = ev.ResetEventId;
            ChanceLastTimeChecked = DateTimeOffset.FromUnixTimeSeconds(0);
            AnnonceType = ev.AnnonceType;
            Discord = ev.Discord;
            InstancedConditionType = ev.InstancedConditionType;
            AreaStartingId = ev.AreaStartingId;
            QuestStartingId = ev.QuestStartingId;
            ParallelLaunch = ev.ParallelLaunch;
            StartEventSound = ev.StartEventSound;
            RandomEventSound = ev.RandomEventSound;
            RemainingTimeEvSound = ev.RemainingTimeEvSound;
            EndEventSound = ev.EndEventSound;
            TPPointID = ev.TPPointID;
            EventFamily = ev.EventFamily;
            TimeBeforeReset = ev.TimeBeforeReset;
            if (TimeBeforeReset > 0)
            {
                ResetFamilyTimer.Interval = ((long)TimeBeforeReset) * 1000;
                ResetFamilyTimer.Elapsed += ResetFamilyTimer_Elapsed;
            }

            if (RandTextInterval.HasValue && RandomText != null && this.EventZones?.Any() == true)
            {
                this.RandomTextTimer.Interval = ((long)RandTextInterval.Value.TotalMinutes).ToTimerMilliseconds();
                this.RandomTextTimer.Elapsed += RandomTextTimer_Elapsed;
                this.RandomTextTimer.AutoReset = true;
                this.HasHandomText = true;
            }

            if (RemainingTimeText != null && RemainingTimeInterval.HasValue && this.EventZones?.Any() == true)
            {
                this.HasRemainingTimeText = true;
                this.RemainingTimeTimer.Interval = ((long)RemainingTimeInterval.Value.TotalMinutes).ToTimerMilliseconds();
                this.RemainingTimeTimer.AutoReset = true;
                this.RemainingTimeTimer.Elapsed += RemainingTimeTimer_Elapsed;
            }

            IsKillingEvent = ev.IsKillingEvent;
            IsTimingEvent = ev.IsTimingEvent;

            Coffres = new List<GameStaticItem>();
            foreach (var coffre in ev.Coffres)
            {
                Coffres.Add(coffre.Copy());
            }
            Mobs = new List<GameNPC>();
            foreach (var mob in ev.Mobs)
            {
                GameNPC newMob = null;
                var mobDef = GameServer.Database.FindObjectByKey<Mob>(mob.InternalID);
                Console.WriteLine("MobDef: " + mobDef.ClassType);
                Type type = ScriptMgr.FindNPCGuildScriptClass(mobDef.Guild, (eRealm)mob.Realm);
                Assembly gasm = Assembly.GetAssembly(typeof(GameServer));
                if (type != null)
                {
                    try
                    {
                        newMob = (GameNPC)type.Assembly.CreateInstance(type.FullName);
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("LoadFromDatabase", e);
                    }
                }
                if (newMob == null)
                {
                    try
                    {
                        newMob = (GameNPC)gasm.CreateInstance(mobDef.ClassType, false);
                    }
                    catch
                    {
                    }
                    if (newMob == null)
                    {
                        foreach (Assembly asm in ScriptMgr.Scripts)
                        {
                            try
                            {
                                newMob = (GameNPC)asm.CreateInstance(mobDef.ClassType, false);
                            }
                            catch
                            {
                            }

                            if (newMob != null)
                                break;
                        }
                    }
                }
                if (newMob == null)
                {
                    newMob = new GameNPC();
                }

                newMob.Name = mob.Name;
                newMob.InternalID = mob.InternalID;
                newMob.GuildName = mobDef.Guild;
                newMob.LoadFromDatabase(mobDef);
                newMob.BuildAmbientTexts();
                newMob.Event = this;
                Mobs.Add(newMob);
            }

            StartEffects = new Dictionary<string, ushort>(ev.StartEffects);
            EndEffects = new Dictionary<string, ushort>(ev.EndEffects);
            RemovedMobs = new Dictionary<string, GameNPC>(ev.RemovedMobs);
            RemovedCoffres = new Dictionary<string, GameStaticItem>(ev.RemovedCoffres);
            InstancedConditionType = ev.InstancedConditionType;
            EndingConditionTypes = ev.EndingConditionTypes;
            EndTime = ev.EndTime;
            ChronoTime = ev.ChronoTime;
            if (ev.AreaConditions != null)
            {
                AreaConditions = new GameEventAreaTrigger(ev.AreaConditions);
                AreaConditions.MasterEvent = this;
            }
        }

        public GameEvent(EventDB db)
        {
            _db = db.Clone();
            ID = db.ObjectId;
            this.RandomTextTimer = new Timer();
            this.RemainingTimeTimer = new Timer();
            this.ResetFamilyTimer = new Timer();
            this.EventFamily = new Dictionary<string, bool>();
            IsInstanceMaster = true;

            AreaXEvent? dbAreaConditions = GameServer.Database.SelectObject<AreaXEvent>(e => e.EventID == this.ID);
            if (dbAreaConditions != null)
            {
                var area = WorldMgr.GetAllRegions().Select(r => r.GetArea(dbAreaConditions.AreaID)).OfType<AbstractArea>().FirstOrDefault();
                if (area == null)
                {
                    log.Warn($"AreaXEvent for {EventName} ({ID}) has invalid area ${dbAreaConditions.AreaID}");
                }
                else
                {
                    AreaConditions = new GameEventAreaTrigger(this, dbAreaConditions, area);
                }
            }

            ParseValuesFromDb(db);

            if (ParallelLaunch)
                Instances = new List<GameEvent>();

            this.Coffres = new List<GameStaticItem>();
            this.Mobs = new List<GameNPC>();
            this.StartEffects = new Dictionary<string, ushort>();
            this.EndEffects = new Dictionary<string, ushort>();
            RemovedMobs = new Dictionary<string, GameNPC>();
            RemovedCoffres = new Dictionary<string, GameStaticItem>();
        }

        private void _Cleanup()
        {
            try
            {
                foreach (var mob in Mobs)
                {
                    if (mob.ObjectState == GameObject.eObjectState.Active)
                    {
                        if (!mob.IsPeaceful)
                            mob.Health = 0;
                        mob.RemoveFromWorld();
                        mob.Delete();
                    }
                }

                foreach (var coffre in Coffres)
                {
                    if (coffre.ObjectState == GameObject.eObjectState.Active)
                        coffre.RemoveFromWorld();
                }
                
                if (this.RandomTextTimer != null)
                {
                    this.RandomTextTimer.Stop();
                }

                if (this.RemainingTimeTimer != null)
                {
                    this.RemainingTimeTimer.Stop();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception while cleaning up event {this}: {ex}");
            }

            List<GameNPC> npc;
            lock (RelatedNPCs)
            {
                npc = new List<GameNPC>(RelatedNPCs);
            }
            foreach (var mob in npc)
            {
                mob.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).Cast<GamePlayer>().ForEach(mob.RefreshEffects);
            }
            AreaConditions?.Reset();

            //Handle Interval Starting Event
            //let a chance to this event to trigger at next interval
            if (StartConditionType == StartingConditionType.Interval)
            {
                Status = EventStatus.Idle;
                StartedTime = null;
                EndTime = null;
                ChanceLastTimeChecked = (DateTimeOffset?)null;
            }
            
            if (IsInstancedEvent)
            {
                var master = GameEventManager.Instance.GetEventByID(ID);
                if (master != null)
                    master.RemoveInstance(this);
            }
            if (!IsRunning)
            {
                RestoreMobs();
            }
        }
        
        private void _Reset()
        {
            var prev = ExchangeStatus(EventStatus.Ending);
            log.DebugFormat("Starting reset of event {0} ({1}) owned by {2} (status is {3})", EventName, ID, Owner, prev);
            try
            {
                StartedTime = (DateTimeOffset?)null;
                EndTime = (DateTimeOffset?)null;
                WantedMobsCount = 0;
                _Cleanup();

                if (IsInstanceMaster && StartConditionType == StartingConditionType.Money)
                {
                    // TODO: review this, this won't work for instanced events...
                    //Reset related NPC Money
                    var moneyNpcDb = GameServer.Database.SelectObjects<MoneyNpcDb>(DB.Column("EventID").IsEqualTo(ID))?.FirstOrDefault();

                    if (moneyNpcDb != null)
                    {
                        var mob = GameServer.Database.FindObjectByKey<Mob>(moneyNpcDb.MobID);

                        if (mob != null)
                        {
                            MoneyEventNPC mobIngame = WorldMgr.Regions[mob.Region].Objects?.FirstOrDefault(o => o?.InternalID?.Equals(mob.ObjectId) == true && o is MoneyEventNPC) as MoneyEventNPC;

                            if (mobIngame != null)
                            {
                                mobIngame.CurrentSilver = 0;
                                mobIngame.CurrentCopper = 0;
                                mobIngame.CurrentGold = 0;
                                mobIngame.CurrentMithril = 0;
                                mobIngame.CurrentPlatinum = 0;
                                mobIngame.SaveIntoDatabase();
                            }
                        }


                        moneyNpcDb.CurrentAmount = 0;
                        GameServer.Database.SaveObject(moneyNpcDb);
                    }

                }

                SaveToDatabase();
            }
            catch (Exception ex)
            {
                log.Error($"Exception while resetting event {EventName} ({ID}): {ex}");
            }
            Status = EventStatus.Idle;
            log.DebugFormat("Finished reset of event {0} ({1}) owned by {2}", EventName, ID, Owner);
        }
        
        public void RemoveInstance(GameEvent instance)
        {
            if (Instances == null)
                return;

            lock (Instances)
            {
                Instances.Remove(instance);
            }
        }

        public void Reset()
        {
            GetInstances().ForEach(ev => ev._Reset());
        }

        private void ApplyEffect(GameObject item, Dictionary<string, ushort> dic)
        {
            foreach (GamePlayer pl in item.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                pl.Out.SendSpellEffectAnimation(item, item, dic[item.InternalID], 0, false, 5);
            }
        }
        
        public async Task<bool> StartEventEffects()
        {
            foreach (var mob in Mobs)
            {
                if (StartEffects.ContainsKey(mob.InternalID))
                {
                    ApplyEffect(mob, StartEffects);
                }
            }

            foreach (var coffre in Coffres)
            {
                if (StartEffects.ContainsKey(coffre.InternalID))
                {
                    ApplyEffect(coffre, StartEffects);
                }
            }

            List<GameNPC> npc;
            lock (RelatedNPCs)
            {
                npc = new List<GameNPC>(RelatedNPCs);
            }
            foreach (var mob in npc)
            {
                mob.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).Cast<GamePlayer>().ForEach(mob.RefreshEffects);
            }

            if (!string.IsNullOrEmpty(StartActionStopEventID))
            {
                var ev = GameEventManager.Instance.GetEventByID(StartActionStopEventID);
                if (ev == null)
                {
                    log.Error(string.Format("Impossible To Stop Event Id {0} from StartActionStopEventID (Event {1}). Event not found", StartActionStopEventID, this.ID));
                }
                else
                {
                    log.Info(string.Format("Stop Event Id {0} from StartActionStopEventID (Event {1})", StartActionStopEventID, this.ID));
                }
                await ev.Stop(EndingConditionType.StartingEvent);
            }
            return true;
        }

        private async Task<bool> _Start(GamePlayer? triggerPlayer)
        {
            log.DebugFormat("Attempting to start event {0} ({1}) for player {2} (IsInstance = {3})", EventName, ID, triggerPlayer, IsInstancedEvent && !IsInstanceMaster);
            var prev = CompareExchangeStatus(EventStatus.Starting, EventStatus.Idle);
            if (prev != EventStatus.Idle)
            {
                log.DebugFormat("Cannot start event {0} ({1}) for player {2} (IsInstance = {3}), status is {4}", EventName, ID, triggerPlayer, IsInstancedEvent && !IsInstanceMaster, prev);
                return false;
            }
            Owner ??= triggerPlayer;
            try
            {
                //temporarly disable
                var eventMaster = GameEventManager.Instance.GetEventByID(ID) ?? this;
                eventMaster.DisableMobs();

                if (StartEventSetup())
                {
                    if (StartEventSound > 0)
                    {
                        foreach (var player in GetPlayersInEventZones(EventZones).Where(IsOwnedBy))
                        {
                            player.Out.SendSoundEffect((ushort)StartEventSound, player.Position, 0);
                        }
                    }

                    //need give more time to client after addtoworld to perform animation
                    await Task.Delay(500);

                    if (TryExchangeStatus(EventStatus.Started, EventStatus.Starting))
                    {
                        await Task.Run(() => StartEventEffects());
                        
                        log.DebugFormat("Event {0} ({1}) started by player {2} (IsInstance = {3})", EventName, ID, triggerPlayer, IsInstancedEvent && !IsInstanceMaster);
                        SaveToDatabase();
                        return true;
                    }
                }
                else
                {
                    log.Warn($"Event startup of {EventName} ({ID}) was aborted: status changed");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception while trying to start event {EventName} ({ID}): {ex}");
            }
            _Reset();
            return false;
        }
        
        private void DisableMobs()
        {
            var disabledMobs = GameServer.Database.SelectObjects<Mob>(DB.Column("RemovedByEventID").IsNotNull());
            foreach (var mob in disabledMobs)
            {
                if (mob.RemovedByEventID.Split("|").Contains(ID.ToString()))
                {
                    var mobInRegion = WorldMgr.Regions[mob.Region].Objects.FirstOrDefault(o => o != null && o is GameNPC npc && npc.InternalID != null && npc.InternalID.Equals(mob.ObjectId));
                    if (mobInRegion != null)
                    {
                        var npcInRegion = mobInRegion as GameNPC;
                        //copy npc
                        RemovedMobs[npcInRegion!.InternalID] = npcInRegion;
                        npcInRegion.RemoveFromWorld();
                        npcInRegion.Delete();
                    }
                }
            }
            var disabledCoffres = GameServer.Database.SelectObjects<DBCoffre>(DB.Column("RemovedByEventID").IsNotNull());
            foreach (var coffre in disabledCoffres)
            {
                if (coffre.RemovedByEventID.Split("|").Contains(ID.ToString()))
                {
                    var coffreInRegion = WorldMgr.Regions[coffre.Region].Objects.FirstOrDefault(o => o != null && o is GameStaticItem item && item.InternalID.Equals(coffre.ObjectId)) as GameStaticItem;
                    if (coffreInRegion != null)
                    {
                        var itemInRegion = coffreInRegion as GameStaticItem;
                        RemovedCoffres[itemInRegion.InternalID] = itemInRegion;
                        itemInRegion.RemoveFromWorld();
                        itemInRegion.Delete();
                    }
                }
            }
        }

        private void RestoreMobs()
        {
            //restore temporarly disabled RemovedMobs
            foreach (var mob in RemovedMobs)
            {
                mob.Value.InternalID = mob.Key;
                mob.Value.AddToWorld();
            }
            RemovedMobs.Clear();

            foreach (var item in RemovedCoffres)
            {
                item.Value.InternalID = item.Key;
                item.Value.AddToWorld();
            }
            RemovedCoffres.Clear();
        }

        public async Task<bool> StartParallel(GamePlayer? triggerPlayer = null)
        {
            if (!TryExchangeStatus(EventStatus.Starting, EventStatus.Idle))
            {
                return false;
            }
            try
            {
                var condition = InstancedConditionType;
                if (condition == InstancedConditionTypes.All)
                    condition = InstancedConditionTypes.Player; // This doesn't seem right to me, but this is what was happening before

                // Keep track of instances we spawn
                Dictionary<object, GamePlayer> playersRegistered = new();
                Func<GamePlayer, object> getKey = (GamePlayer p) =>
                {
                    switch (condition)
                    {
                        case InstancedConditionTypes.All:
                            return this;

                        case InstancedConditionTypes.Player:
                            return p;

                        case InstancedConditionTypes.Group:
                            return p.Group ?? (object)p;

                        case InstancedConditionTypes.Guild:
                            return p.Guild?.IsSystemGuild == false ? p.Guild : p;

                        case InstancedConditionTypes.Battlegroup:
                            return p.BattleGroup ?? (object)p;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                };

                // Register all players in the area, TODO: There has to be a better way to do this?
                foreach (var player in WorldMgr.GetAllPlayingClients().Select(c => c.Player).Where(p => p.CurrentAreas.OfType<AbstractArea>().Any(a => a.DbArea.ObjectId == AreaStartingId))) // 
                {
                    var key = getKey(player);
                    playersRegistered.TryAdd(key, player);
                }

                // Unregister players with running instances, prepare to start existing ready instances
                List<GameEvent> startingExistingInstances = new();
                lock (Instances)
                {
                    foreach (var i in Instances)
                    {
                        playersRegistered.Remove(getKey(i.Owner));
                        if (i.IsReady)
                            startingExistingInstances.Add(i);
                    }
                }

                if (playersRegistered.Count > 0 || startingExistingInstances.Count > 0)
                {
                    // Spawn an instance for every registered player
                    List<GameEvent> spawnedInstances = new();
                    foreach (GamePlayer pl in playersRegistered.Values)
                    {
                        try
                        {
                            spawnedInstances.Add(Instantiate(pl));
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Could not instantiate event {this.EventName} ({ID}) for player {pl}: {ex}");
                        }
                    }

                    List<(GameEvent ev, Task<bool> task)> allTasks = new();
                    foreach (GameEvent e in startingExistingInstances.Concat(spawnedInstances))
                    {
                        // Start all events...
                        allTasks.Add((e, e._Start(triggerPlayer)));
                    }
                    foreach (var entry in allTasks)
                    {
                        try
                        {
                            // Await all startups
                            await entry.task;
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Could not start event {this.EventName} ({ID}) instance owned by {entry.ev.Owner}: {ex}");
                            entry.ev.Reset();
                        }
                    }
                    lock (Instances)
                    {
                        Instances.AddRange(spawnedInstances.Where(i => i.IsRunning));
                    }
                }
                Status = EventStatus.Idle;
            }
            catch (Exception ex)
            {
                log.Error($"Exception while starting event {this.EventName} ({ID}): {ex}");
                Reset();
                return false;
            }
            return true;
        }

        public async Task<bool> Start(GamePlayer? triggerPlayer = null)
        {
            if (ParallelLaunch)
            {
                if (IsInstanceMaster)
                    return await StartParallel(triggerPlayer);
                else
                {
                    GameEvent master = GameEventManager.Instance.GetEventByID(this.ID);
                    if (master != null)
                        return await master.StartParallel(triggerPlayer);
                }
            }
            if (IsInstanceMaster)
            {
                GameEvent instance = GetOrCreateInstance(triggerPlayer);
                if (instance == null)
                {
                    return false;
                }
                return await instance._Start(triggerPlayer);
            }
            return await _Start(triggerPlayer);
        }

        private async Task ShowEndEffects()
        {
            foreach (var mob in Mobs)
            {
                if (EndEffects.ContainsKey(mob.InternalID))
                {
                    this.ApplyEffect(mob, EndEffects);
                }
            }

            foreach (var coffre in Coffres)
            {
                if (EndEffects.ContainsKey(coffre.InternalID))
                {
                    this.ApplyEffect(coffre, EndEffects);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        public async Task Stop(EndingConditionType end)
        {
            var prev = ExchangeStatus(EventStatus.Ending);
            log.DebugFormat("Attempting to end event {0} ({1}) by player {2} (IsInstance = {3}), was {4}", EventName, ID, Owner, IsInstancedEvent && !IsInstanceMaster, prev);
            try
            {
                EndTime = DateTimeOffset.UtcNow;
                if (EndText != null && EventZones?.Any() == true)
                {
                    SendEventNotification((string lang) => FormatEventMessage(GetFormattedEndText(lang, Owner)), (Discord == 2 || Discord == 3), true);

                    foreach (var player in GetPlayersInEventZones(EventZones))
                    {
                        if (EndEventSound > 0)
                        {
                            player.Out.SendSoundEffect((ushort)EndEventSound, player.Position, 0);
                        }
                    }

                    //Enjoy the message
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

                if (end == EndingConditionType.Kill && IsKillingEvent)
                {
                    Status = EventStatus.EndedByKill;
                    //Allow time to loot
                    await Task.Delay(TimeSpan.FromSeconds(15));
                    await ShowEndEffects();
                    _Cleanup();
                }
                else if (end == EndingConditionType.StartingEvent)
                {
                    Status = EventStatus.EndedByEventStarting;
                    await ShowEndEffects();
                    _Cleanup();
                }
                else if (end == EndingConditionType.Timer)
                {
                    Status = EventStatus.EndedByTimer;
                    await ShowEndEffects();
                    _Cleanup();
                }
                else if (end == EndingConditionType.AreaEvent)
                {
                    Status = EventStatus.EndedByAreaEvent;
                    await ShowEndEffects();
                    _Cleanup();
                }
                else if (end == EndingConditionType.TextNPC)
                {
                    Status = EventStatus.EndedByTextNPC;
                    await ShowEndEffects();
                    _Cleanup();
                }
                else if (end == EndingConditionType.Switch)
                {
                    Status = EventStatus.EndedBySwitch;
                    await ShowEndEffects();
                    _Cleanup();
                }

                //Handle Consequences
                //Consequence A
                if (EndingConditionTypes.Count() == 1 || (EndingConditionTypes.Count() > 1 && EndingConditionTypes.First() == end))
                {
                    await GameEventManager.Instance.HandleConsequence(EndingActionA, EventZones, EndActionStartEventID, ResetEventId, this);
                }
                else
                {
                    //Consequence B
                    await GameEventManager.Instance.HandleConsequence(EndingActionB, EventZones, EndActionStartEventID, ResetEventId, this);
                }

                log.Info(string.Format("Event Id: {0}, Name: {1} was stopped At: {2}", ID, EventName, DateTime.Now.ToString()));

                SaveToDatabase();
            }
            finally
            {
                Status = EventStatus.Idle;

                //Handle Interval Starting Event
                //let a chance to this event to trigger at next interval
                if (StartConditionType == StartingConditionType.Interval)
                {
                    Status = EventStatus.Idle;
                    StartedTime = null;
                    EndTime = null;
                    ChanceLastTimeChecked = (DateTimeOffset?)null;
                }
            }
            log.DebugFormat("Finished event {0} ({1}) owned by {2}", EventName, ID, Owner);
        }

        public void SendEventNotification(Func<string, string> message, bool sendDiscord, bool createNews = false)
        {
            string lang = Properties.SERV_LANGUAGE!;
            string msg = message(lang) ?? string.Empty;
            Dictionary<string, string> cachedMessages = new Dictionary<string, string>(8)
            {
                { lang, msg }
            };
            
            if (!String.IsNullOrEmpty(msg))
            {
                if (Properties.DISCORD_ACTIVE && sendDiscord)
                {
                    var hook = new DolWebHook(Properties.DISCORD_WEBHOOK_ID);
                    hook.SendMessage(msg);
                }
                if (createNews)
                {
                    foreach (string l in LanguageMgr.GetAllSupportedLanguages())
                    {
                        if (!cachedMessages.ContainsKey(l))
                            cachedMessages[l] = message(l);
                    }
                    NewsMgr.CreateNews(cachedMessages, 0, eNewsType.RvRLocal, false);
                }
            }
            foreach (var player in GetPlayersInEventZones(EventZones))
            {
                lang = player?.Client?.Account?.Language ?? Properties.SERV_LANGUAGE!;
                if (!cachedMessages.TryGetValue(lang, out msg))
                {
                    msg = message(lang);
                    if (string.IsNullOrEmpty(msg))
                        msg = message(Properties.SERV_LANGUAGE);
                    cachedMessages[lang] = msg;
                }
                NotifyPlayer(player, AnnonceType, msg);
            }
        }
        
        public static void NotifyPlayer(GamePlayer player, AnnonceType annonceType, string message)
        {
            eChatType type;
            eChatLoc loc;

            switch (annonceType)
            {
                case AnnonceType.Log:
                    type = eChatType.CT_Merchant;
                    loc = eChatLoc.CL_SystemWindow;
                    break;

                case AnnonceType.Send:
                    type = eChatType.CT_Send;
                    loc = eChatLoc.CL_SystemWindow;
                    break;

                case AnnonceType.Windowed:
                    type = eChatType.CT_System;
                    loc = eChatLoc.CL_PopupWindow;
                    break;

                default:
                    type = eChatType.CT_ScreenCenter;
                    loc = eChatLoc.CL_SystemWindow;
                    break;
            }
            
            if (annonceType == AnnonceType.Confirm)
            {
                player.Out.SendDialogBox(eDialogCode.CustomDialog, 0, 0, 0, 0, eDialogType.Ok, true, message);
            }
            else
            {
                player.Out.SendMessage(message, type, loc);
            }
        }

        public GameEvent Instantiate(GamePlayer owner)
        {
            log.DebugFormat("Instantiating event {0} {1} for player {2}", EventName, ID, owner);
            GameEvent ret = new GameEvent(this);

            ret.Owner = owner;
            return ret;
        }
        
        private bool StartEventSetup()
        {
            WantedMobsCount = 0;
            if (EndingConditionTypes?.Contains(EndingConditionType.Timer) == true)
            {
                if (TimerType == TimerType.ChronoType)
                {
                    EndTime = DateTimeOffset.UtcNow.AddMinutes(ChronoTime);
                }
                else
                {
                    //Cannot launch event if Endate is not set in DateType and no other ending exists
                    if (!EndTime.HasValue)
                    {
                        if (EndingConditionTypes.Count() == 1)
                        {
                            log.Error(string.Format("Cannot Launch Event {0}, Name: {1} with DateType because EndDate is Null", ID, EventName));
                            return false;
                        }
                        else
                        {
                            log.Warn(string.Format("Event Id: {0}, Name: {1}, started with ending type Timer DateType but Endate is Null, Event Started with other endings", ID, EventName));
                        }
                    }
                }
            }

            foreach (var mob in Mobs)
            {
                mob.Health = mob.MaxHealth;
                mob.Event = this;
                var db = GameServer.Database.FindObjectByKey<Mob>(mob.InternalID);
                mob.LoadFromDatabase(db);

                var dbGroupMob = GameServer.Database.SelectObjects<GroupMobXMobs>(DB.Column("MobID").IsEqualTo(mob.InternalID))?.FirstOrDefault();

                if (dbGroupMob != null)
                {
                    MobGroup mobGroup;
                    if (MobGroupManager.Instance.Groups.TryGetValue(dbGroupMob.GroupId, out mobGroup))
                    {
                        mob.AddToMobGroup(mobGroup);
                        if (!mobGroup.NPCs.Contains(mob))
                        {
                            mobGroup.NPCs.Add(mob);
                            mobGroup.ApplyGroupInfos();
                        }
                    }
                    else
                    {
                        var mobgroupDb = GameServer.Database.FindObjectByKey<GroupMobDb>(dbGroupMob.GroupId);
                        if (mobgroupDb != null)
                        {
                            var groupInteraction = mobgroupDb.GroupMobInteract_FK_Id != null ?
                            GameServer.Database.SelectObjects<GroupMobStatusDb>(DB.Column("GroupStatusId").IsEqualTo(mobgroupDb.GroupMobInteract_FK_Id))?.FirstOrDefault() : null;

                            var groupOriginStatus = mobgroupDb.GroupMobOrigin_FK_Id != null ?
                            GameServer.Database.SelectObjects<GroupMobStatusDb>(DB.Column("GroupStatusId").IsEqualTo(mobgroupDb.GroupMobOrigin_FK_Id))?.FirstOrDefault() : null;
                            mobGroup = new MobGroup(mobgroupDb, groupInteraction, groupOriginStatus);
                            MobGroupManager.Instance.Groups.Add(dbGroupMob.MobID, mobGroup);
                            mobGroup.NPCs.Add(mob);
                            mob.AddToMobGroup(mobGroup);
                            mobGroup.ApplyGroupInfos();
                        }
                    }
                }

                if (IsKillingEvent && MobNamesToKill.Contains(mob.Name))
                {
                    WantedMobsCount++;
                }
            }

            if (IsKillingEvent)
            {
                int delta = MobNamesToKill.Count() - WantedMobsCount;

                if (WantedMobsCount == 0 && EndingConditionTypes.Where(ed => ed != EndingConditionType.Kill).Count() == 0)
                {
                    log.Error(string.Format("Event ID: {0}, Name: {1}, cannot be start because No Mobs found for Killing Type ending and no other ending type set", ID, EventName));
                    return false;
                }
                else if (delta > 0)
                {
                    log.Error(string.Format("Event ID: {0}, Name {1}: with Kill type has {2} mobs missings, MobNamesToKill column in datatabase and tagged mobs Name should match.", ID, EventName, delta));
                }
            }


            if (DebutText != null && EventZones?.Any() == true)
            {
                SendEventNotification((lang) => FormatEventMessage(GetFormattedDebutText(lang, Owner)), (Discord == 1 || Discord == 3), true);
            }

            if (HasHandomText)
            {
                RandomTextTimer.Start();
            }

            if (HasRemainingTimeText)
            {
                RemainingTimeTimer.Start();
            }

            foreach (var mob in Mobs)
            {
                mob.AddToWorld();
            }

            Coffres.ForEach(c => c.AddToWorld());
            StartedTime = DateTimeOffset.UtcNow;
            return true;
        }

        public void ParseValuesFromDb(EventDB db)
        {
            EventAreas = !string.IsNullOrEmpty(db.EventAreas) ? db.EventAreas.Split(new char[] { '|' }) : null;
            EventChance = db.EventChance;
            EventName = db.EventName;
            EventZones = !string.IsNullOrEmpty(db.EventZones) ? db.EventZones.Split(new char[] { '|' }) : null;
            ShowEvent = db.ShowEvent;
            StartConditionType = Enum.TryParse(db.StartConditionType.ToString(), out StartingConditionType st) ? st : StartingConditionType.Timer;
            EventChanceInterval = db.EventChanceInterval > 0 && db.EventChanceInterval < long.MaxValue ? TimeSpan.FromMinutes(db.EventChanceInterval) : (TimeSpan?)null;
            DebutText = !string.IsNullOrEmpty(db.DebutText) ? db.DebutText : null;
            EndText = !string.IsNullOrEmpty(db.EndText) ? db.EndText : null;
            StartedTime = (DateTimeOffset?)null;
            EndingConditionTypes = db.EndingConditionTypes.Split(new char[] { '|' }).Select(c => Enum.TryParse(c, out EndingConditionType end) ? end : GameEvents.EndingConditionType.Timer);
            RandomText = !string.IsNullOrEmpty(db.RandomText) ? db.RandomText.Split(new char[] { '|' }) : null;
            RandTextInterval = db.RandTextInterval > 0 && db.RandTextInterval < long.MaxValue ? TimeSpan.FromMinutes(db.RandTextInterval) : (TimeSpan?)null;
            RemainingTimeInterval = db.RemainingTimeInterval > 0 && db.RemainingTimeInterval < long.MaxValue ? TimeSpan.FromMinutes(db.RemainingTimeInterval) : (TimeSpan?)null;
            RemainingTimeText = !string.IsNullOrEmpty(db.RemainingTimeText) ? db.RemainingTimeText : null;
            EndingActionA = Enum.TryParse(db.EndingActionA.ToString(), out EndingAction endActionA) ? endActionA : EndingAction.None;
            EndingActionB = Enum.TryParse(db.EndingActionB.ToString(), out EndingAction endActionB) ? endActionB : EndingAction.None;
            MobNamesToKill = !string.IsNullOrEmpty(db.MobNamesToKill) ? db.MobNamesToKill.Split(new char[] { '|' }) : null;
            EndActionStartEventID = !string.IsNullOrEmpty(db.EndActionStartEventID) ? db.EndActionStartEventID : null;
            StartActionStopEventID = !string.IsNullOrEmpty(db.StartActionStopEventID) ? db.StartActionStopEventID : null;
            StartTriggerTime = db.StartTriggerTime > 0 && db.StartTriggerTime < long.MaxValue ? DateTimeOffset.FromUnixTimeSeconds(db.StartTriggerTime) : (DateTimeOffset?)null;
            TimerType = Enum.TryParse(db.TimerType.ToString(), out TimerType timer) ? timer : TimerType.DateType;
            EndTime = (DateTimeOffset?)null;
            ChronoTime = db.ChronoTime;
            KillStartingGroupMobId = !string.IsNullOrEmpty(db.KillStartingGroupMobId) ? db.KillStartingGroupMobId : null;
            ResetEventId = !string.IsNullOrEmpty(db.ResetEventId) ? db.ResetEventId : null;
            ChanceLastTimeChecked = DateTimeOffset.FromUnixTimeSeconds(0);
            AnnonceType = Enum.TryParse(db.AnnonceType.ToString(), out AnnonceType a) ? a : AnnonceType.Center;
            Discord = db.Discord;
            InstancedConditionType = Enum.TryParse(db.InstancedConditionType.ToString(), out InstancedConditionTypes inst) ? inst : InstancedConditionTypes.All;
            AreaStartingId = !string.IsNullOrEmpty(db.AreaStartingId) ? db.AreaStartingId : null;
            QuestStartingId = !string.IsNullOrEmpty(db.QuestStartingId) ? db.QuestStartingId : null;
            ParallelLaunch = db.ParallelLaunch;
            StartEventSound = db.StartEventSound;
            RandomEventSound = db.RandomEventSound;
            RemainingTimeEvSound = db.RemainingTimeEvSound;
            EndEventSound = db.EndEventSound;
            TPPointID = db.TPPointID;

            // get kes from string[] db.EventFamily, and set values to false 
            if (db.EventFamily != null)
                foreach (string family in db.EventFamily.Split('|'))
                    EventFamily.Add(family, false);
            if (db.TimerBeforeReset != 0)
            {
                TimeBeforeReset = db.TimerBeforeReset;
                ResetFamilyTimer.Interval = ((long)TimeBeforeReset) * 1000;
                ResetFamilyTimer.Elapsed += ResetFamilyTimer_Elapsed;
            }

            //Handle invalid ChronoType
            if (TimerType == TimerType.ChronoType && ChronoTime <= 0)
            {
                //Define 5 minutes by default
                log.Error(string.Format("Event with Chrono Timer tpye has wrong value: {0}, value set to 5 minutes instead", ChronoTime));
                ChronoTime = 5;
            }

            if (StartConditionType == StartingConditionType.Kill && KillStartingGroupMobId == null)
            {
                log.Error(string.Format("Event Id: {0}, Name: {1}, with kill Starting Type will not start because KillStartingMob is Null", ID, EventName));
            }

            if (RandTextInterval.HasValue && RandomText != null && this.EventZones?.Any() == true)
            {
                this.RandomTextTimer.Interval = ((long)RandTextInterval.Value.TotalMinutes).ToTimerMilliseconds();
                this.RandomTextTimer.Elapsed += RandomTextTimer_Elapsed;
                this.RandomTextTimer.AutoReset = true;
                this.HasHandomText = true;
            }

            if (RemainingTimeText != null && RemainingTimeInterval.HasValue && this.EventZones?.Any() == true)
            {
                this.HasRemainingTimeText = true;
                this.RemainingTimeTimer.Interval = ((long)RemainingTimeInterval.Value.TotalMinutes).ToTimerMilliseconds();
                this.RemainingTimeTimer.AutoReset = true;
                this.RemainingTimeTimer.Elapsed += RemainingTimeTimer_Elapsed;
            }

            if (MobNamesToKill?.Any() == true && EndingConditionTypes.Contains(EndingConditionType.Kill))
            {
                IsKillingEvent = true;
            }

            if (EndTime.HasValue && EndingConditionTypes.Contains(EndingConditionType.Timer))
            {
                IsTimingEvent = true;
            }
        }

        public bool IsOwnedBy(GamePlayer player)
        {
            switch (InstancedConditionType)
            {
                case InstancedConditionTypes.All:
                    return true;

                case InstancedConditionTypes.Player:
                    return Owner == player;

                case InstancedConditionTypes.Group:
                    return Owner?.Group != null ? player.Group == Owner.Group : player == Owner;

                case InstancedConditionTypes.Guild:
                    return Owner?.Guild?.IsSystemGuild == false ? player.Guild == Owner.Guild : player == Owner;

                case InstancedConditionTypes.Battlegroup:
                    return Owner?.BattleGroup != null ? player.BattleGroup == Owner.BattleGroup : player == Owner;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public string FormatEventMessage(string message)
        {
            if (Owner == null)
                return message;
            
            if (Owner.Guild is not { GuildType: Guild.eGuildType.ServerGuild })
            {
                message = message.Replace("<guilde>", Owner.GuildName);
            }
            if (message.Contains("<player>"))
            {
                message = message.Replace("<player>", Owner.Name);
            }
            if (Owner.Group != null)
            {
                message = message.Replace("<group>", Owner.Group.Leader.Name);
            }
            if (Owner != null)
            {
                message = message.Replace("<race>", Owner.RaceName);
            }
            if (Owner != null)
            {
                message = message.Replace("<class>", Owner.CharacterClass.Name);
            }
            return message;
        }

        public bool IsVisibleTo(GameObject obj)
        {
            if (Owner == obj)
                return true;
            
            switch (InstancedConditionType)
            {
                case InstancedConditionTypes.All:
                    return true;
                case InstancedConditionTypes.Player:
                    return Owner == obj;
                case InstancedConditionTypes.Group:
                    return obj is GameLiving living && Owner?.Group?.IsInTheGroup(living) == true;
                case InstancedConditionTypes.Guild:
                    return Owner?.Guild is { IsSystemGuild: false } && (obj as GamePlayer)?.Guild == Owner.Guild;
                case InstancedConditionTypes.Battlegroup:
                    return obj is GamePlayer player && Owner?.BattleGroup?.IsInTheBattleGroup(player) == true;
                default:
                    return false; // Unknown instance type
            }
        }

        private IEnumerable<GamePlayer> GetPlayersInEventZones(IEnumerable<string> eventZones)
        {
            return WorldMgr.GetAllPlayingClients()
                .Where(c => eventZones.Contains(c.Player.CurrentZone.ID.ToString()))
                .Select(c => c.Player);
        }

        private void RemainingTimeTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var player in GetPlayersInEventZones(this.EventZones))
            {
                string message = this.GetFormattedRemainingTimeText(player.Client.Account.Language, player);
                NotifyPlayer(player, AnnonceType, message);
            }

            if (this.RemainingTimeEvSound > 0)
            {
                foreach (var player in GetPlayersInEventZones(this.EventZones))
                {
                    player.Out.SendSoundEffect((ushort)this.RemainingTimeEvSound, player.Position, 0);
                }
            }
        }

        private void RandomTextTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var rand = new Random(DateTime.Now.Millisecond);
            int index = rand.Next(0, this.RandomText.Count());

            foreach (var player in GetPlayersInEventZones(this.EventZones))
            {
                string message = this.GetFormattedRandomText(player.Client.Account.Language, player);
                NotifyPlayer(player, this.AnnonceType, message);
            }

            if (!string.IsNullOrEmpty(this.RandomEventSound))
            {
                var sounds = this.RandomEventSound.Split('|').Select(int.Parse).ToArray();
                int soundIndex = rand.Next(0, sounds.Length);
                foreach (var player in GetPlayersInEventZones(this.EventZones))
                {
                    player.Out.SendSoundEffect((ushort)sounds[soundIndex], player.Position, 0);
                }
            }
        }

        private void ResetFamilyTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ResetFamilyTimer.Stop();
            foreach (var family in EventFamily)
            {
                if (EventFamily[family.Key])
                {
                    GameEventManager.Instance.ResetEventsFromId(family.Key);
                    EventFamily[family.Key] = false;
                }
            }
        }

        public string ID
        {
            get;
            set;
        }

        public string EventName
        {
            get;
            set;
        }

        public IEnumerable<string> EventAreas
        {
            get;
            set;
        }

        public IEnumerable<string> EventZones
        {
            get;
            set;
        }

        public IEnumerable<string> MobNamesToKill
        {
            get;
            set;
        }

        public string ResetEventId
        {
            get;
            set;
        }

        public bool HasHandomText
        {
            get;
            set;
        }

        public TimerType TimerType
        {
            get;
            set;
        }

        public long ChronoTime
        {
            get;
            set;
        }

        public string KillStartingGroupMobId
        {
            get;
            set;
        }

        public DateTimeOffset? ChanceLastTimeChecked
        {
            get;
            set;
        }

        public bool HasRemainingTimeText
        {
            get;
            set;
        }

        public bool IsKillingEvent
        {
            get;
            set;
        }

        public bool IsTimingEvent
        {
            get;
            set;
        }

        public bool ShowEvent
        {
            get;
            set;
        }

        public StartingConditionType StartConditionType
        {
            get;
            set;
        }


        public IEnumerable<EndingConditionType> EndingConditionTypes
        {
            get;
            set;
        }

        public int EventChance
        {
            get;
            set;
        }

        public TimeSpan? EventChanceInterval
        {
            get;
            set;
        }

        public AnnonceType AnnonceType
        {
            get;
            set;
        }

        public int Discord
        {
            get;
            set;
        }

        public InstancedConditionTypes InstancedConditionType
        {
            get;
            set;
        }

        public string AreaStartingId
        {
            get;
            set;
        }

        public string QuestStartingId
        {
            get;
            set;
        }

        public int WantedMobsCount
        {
            get;
            set;
        }

        public string DebutText
        {
            get;
            set;
        }

        public int StartEventSound { get; set; }

        public string EndActionStartEventID
        {
            get;
            set;
        }

        public string StartActionStopEventID
        {
            get;
            set;
        }

        public IEnumerable<string> RandomText
        {
            get;
            set;
        }

        public string RandomEventSound { get; set; }

        public TimeSpan? RandTextInterval
        {
            get;
            set;
        }

        public string RemainingTimeText
        {
            get;
            set;
        }

        public int RemainingTimeEvSound { get; set; }

        public TimeSpan? RemainingTimeInterval
        {
            get;
            set;
        }

        public string EndText
        {
            get;
            set;
        }

        public int EndEventSound { get; set; }

        private int _status = (int)EventStatus.Idle;

        private EventStatus CompareExchangeStatus(EventStatus desired, EventStatus expected)
        {
            return (EventStatus)Interlocked.CompareExchange(ref _status, (int)desired, (int)expected);
        }

        private EventStatus ExchangeStatus(EventStatus desired)
        {
            return (EventStatus)Interlocked.Exchange(ref _status, (int)desired);
        }
        
        private bool TryExchangeStatus(EventStatus desired, EventStatus expected)
        {
            return ((EventStatus)Interlocked.CompareExchange(ref _status, (int)desired, (int)expected)) == expected;
        }

        public EventStatus Status
        {
            get => (EventStatus)_status;
            private set => _status = (int)value;
        }

        public EndingAction EndingActionA
        {
            get;
            set;
        }


        public EndingAction EndingActionB
        {
            get;
            set;
        }

        public DateTimeOffset? EndTime
        {
            get;
            set;
        }

        public DateTimeOffset? StartedTime
        {
            get;
            set;
        }


        public DateTimeOffset? StartTriggerTime
        {
            get;
            set;
        }

        public List<GameNPC> Mobs
        {
            get;
        }

        public List<GameStaticItem> Coffres
        {
            get;
        }

        public bool ParallelLaunch
        {
            get;
            set;
        }

        public bool IsInstancedEvent => ParallelLaunch;
        
        public bool IsInstanceMaster { get; set; }
        
        public List<GameEvent> Instances { get; init; }

        public Dictionary<string, bool> EventFamily
        {
            get;
            set;
        }

        public int TimeBeforeReset
        {
            get;
            set;
        }

        public int? TPPointID { get; set; }

        public string GetFormattedDebutText(string language, GamePlayer player)
        {
            if (string.IsNullOrEmpty(DebutText))
                return string.Empty;

            return LanguageMgr.GetEventMessage(language, DebutText, player?.Name ?? string.Empty);
        }

        public string GetFormattedEndText(string language, GamePlayer player)
        {
            if (string.IsNullOrEmpty(EndText))
                return string.Empty;

            if (player == null)
                return LanguageMgr.GetEventMessage(language, EndText, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            
            string playerName = player.Name;
            string groupName = player.Group?.Leader?.Name ?? "???";
            string guildName = player.GuildName ?? "???";
            string className = player.CharacterClass?.Name ?? "???";
            string raceName = player.RaceName ?? "???";

            return LanguageMgr.GetEventMessage(language, EndText ?? string.Empty, playerName, groupName, guildName, className, raceName);
        }

        public string GetFormattedRemainingTimeText(string language, GamePlayer player)
        {
            if (string.IsNullOrEmpty(RemainingTimeText))
                return string.Empty;

            if (player == null)
                return LanguageMgr.GetEventMessage(language, RemainingTimeText);

            return LanguageMgr.GetEventMessage(language, RemainingTimeText, player.Name);
        }

        public string GetFormattedRandomText(string language, GamePlayer player)
        {
            if (RandomText?.Any() != true)
                return string.Empty;
            
            var rand = new Random(DateTime.Now.Millisecond);
            string message = RandomText.ElementAt(rand.Next(0, RandomText.Count()));
            return LanguageMgr.GetEventMessage(language, message, player?.Name);
        }

        public GamePlayer Owner
        {
            get => owner;
            set => owner = value;
        }

        public void SaveToDatabase()
        {
            if (!IsInstanceMaster)
                return;
            
            var db = _db as EventDB;
            bool needClone = false;

            if (db == null)
            {
                db = new EventDB();
                needClone = true;
            }

            db.EventAreas = EventAreas != null ? string.Join("|", EventAreas) : null;
            db.EventChance = EventChance;
            db.EventName = EventName;
            db.EventZones = EventZones != null ? string.Join("|", EventZones) : null;
            db.ShowEvent = ShowEvent;
            db.StartConditionType = (int)StartConditionType;
            db.EndingConditionTypes = string.Join("|", EndingConditionTypes.Select(t => ((int)t).ToString()));
            db.EventChanceInterval = EventChanceInterval.HasValue ? (long)EventChanceInterval.Value.TotalMinutes : 0;
            db.DebutText = !string.IsNullOrEmpty(DebutText) ? DebutText : null;
            db.EndText = EndText;
            db.StartedTime = StartedTime?.ToUnixTimeSeconds() ?? 0;
            db.EndTime = EndTime.HasValue ? EndTime.Value.ToUnixTimeSeconds() : 0;
            db.RandomText = RandomText != null ? string.Join("|", RandomText) : null;
            db.RandTextInterval = RandTextInterval.HasValue ? (long)RandTextInterval.Value.TotalMinutes : 0;
            db.RemainingTimeInterval = RemainingTimeInterval.HasValue ? (long)RemainingTimeInterval.Value.TotalMinutes : 0;
            db.RemainingTimeText = !string.IsNullOrEmpty(RemainingTimeText) ? RemainingTimeText : null;
            db.EndingActionA = (int)EndingActionA;
            db.EndingActionB = (int)EndingActionB;
            db.StartActionStopEventID = !string.IsNullOrEmpty(StartActionStopEventID) ? StartActionStopEventID : null;
            db.EndActionStartEventID = EndActionStartEventID;
            db.MobNamesToKill = MobNamesToKill != null ? string.Join("|", MobNamesToKill) : null;
            db.Status = (int)Status;
            db.StartTriggerTime = StartTriggerTime.HasValue ? StartTriggerTime.Value.ToUnixTimeSeconds() : 0;
            db.ChronoTime = ChronoTime;
            db.TimerType = (int)this.TimerType;
            db.KillStartingGroupMobId = KillStartingGroupMobId;
            db.ResetEventId = ResetEventId;
            db.ChanceLastTimeChecked = ChanceLastTimeChecked.HasValue ? ChanceLastTimeChecked.Value.ToUnixTimeSeconds() : 0;
            db.AnnonceType = (byte)AnnonceType;
            db.Discord = Discord;
            db.InstancedConditionType = (int)InstancedConditionType;
            db.AreaStartingId = AreaStartingId;
            db.QuestStartingId = QuestStartingId;
            db.StartEventSound = StartEventSound;
            db.RandomEventSound = RandomEventSound;
            db.RemainingTimeEvSound = RemainingTimeEvSound;
            db.EndEventSound = EndEventSound;
            db.TPPointID = TPPointID;

            if (ID == null)
            {
                GameServer.Database.AddObject(db);
                ID = db.ObjectId;
            }
            else
            {
                db.ObjectId = ID;
                GameServer.Database.SaveObject(db);
            }

            if (needClone)
                _db = db.Clone();
        }

        public IEnumerable<GameEvent> GetInstances()
        {
            if (IsInstancedEvent && IsInstanceMaster)
            {
                lock (Instances)
                {
                    return Instances.ToList();
                }
            }
            else
            {
                return new[] { this };
            }
        }

        public GameEvent? GetInstance(GameObject objectFor)
        {
            if (objectFor is GameLiving living)
            {
                var controller = living.GetController();
                if (controller is GameNPC eventNPC)
                    return eventNPC.Event;

                if (controller is GamePlayer player)
                    return GetInstances().FirstOrDefault(i => i.IsOwnedBy(player));
            }
            return null;
        }
        
        public GameEvent GetOrCreateInstance(GamePlayer? triggerPlayer)
        {
            if (IsInstancedEvent)
            {
                if (!IsInstanceMaster)
                {
                    return GameEventManager.Instance.GetEventByID(ID)?.GetOrCreateInstance(triggerPlayer);
                }
                lock (Instances)
                {
                    GameEvent instance = Instances.FirstOrDefault(i => i.IsOwnedBy(triggerPlayer));
                    if (instance == null)
                    {
                        instance = Instantiate(triggerPlayer);
                        Instances.Add(instance);
                    }
                    return instance;
                }
            }
            else
            {
                return this;
            }
        }

        public bool IsReady => ParallelLaunch || Status == EventStatus.Idle;

        public bool IsRunning
        {
            get
            {
                if (IsInstancedEvent && IsInstanceMaster)
                {
                    lock (Instances)
                    {
                        return Instances.Any(e => e.IsRunning);
                    }
                }
                else
                {
                    return Status is EventStatus.Started or EventStatus.Starting;
                }
            }
        }
    }
}