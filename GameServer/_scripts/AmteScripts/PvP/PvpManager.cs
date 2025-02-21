﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using AmteScripts.Areas;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Geometry;
using DOL.Territories;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;
using AmteScripts.PvP;
using DOL.GS.Scripts;
using static DOL.GS.Area;
using AmteScripts.PvP.CTF;
using Discord;
using Google.Protobuf.WellKnownTypes;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using static AmteScripts.Managers.PvpManager;
using static System.Formats.Asn1.AsnWriter;
using static DOL.GameEvents.GameEvent;

namespace AmteScripts.Managers
{
    public class PvpManager
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        // Time window, e.g. 14:00..22:00
        private static readonly TimeSpan _startTime = new TimeSpan(14, 0, 0);
        private static readonly TimeSpan _endTime = _startTime.Add(TimeSpan.FromHours(8));
        private const int _checkInterval = 30_000; // 30 seconds
        private static RegionTimer _timer;

        private bool _isOpen;
        private bool _isForcedOpen;
        private ushort _currentRegionID;
        private List<Zone> _zones = new();

        /// <summary>The chosen session from DB for the day</summary>
        private PvpSession _activeSession;

        // Scoreboard
        private Dictionary<string, PlayerScore> _playerScores = new Dictionary<string, PlayerScore>();
        private Dictionary<Guild, GroupScore> _groupScores = new Dictionary<Guild, GroupScore>();

        // Queues
        private List<GamePlayer> _soloQueue = new List<GamePlayer>();
        private List<GamePlayer> _groupQueue = new List<GamePlayer>();

        // For realm-based spawns
        private Dictionary<eRealm, List<GameNPC>> _spawnNpcsRealm = new Dictionary<eRealm, List<GameNPC>>() { { eRealm.Albion, new List<GameNPC>() }, { eRealm.Midgard, new List<GameNPC>() }, { eRealm.Hibernia, new List<GameNPC>() }, };
        // For random spawns (all spawns in session's zones)
        private List<GameNPC> _spawnNpcsGlobal = new List<GameNPC>();
        // For "RandomLock" so we don't reuse the same spawn
        private HashSet<GameNPC> _usedSpawns = new HashSet<GameNPC>();
        // Here we track solo-based safe areas (player => area)
        private Dictionary<GamePlayer, AbstractArea> _soloAreas = new Dictionary<GamePlayer, AbstractArea>();
        // And group-based safe areas (group => area)
        private Dictionary<Group, AbstractArea> _groupAreas = new Dictionary<Group, AbstractArea>();
        // Key = the group object, Value = the ephemeral guild we created
        private Dictionary<Group, Guild> _groupGuilds = new Dictionary<Group, Guild>();
        // Ephemeral guilds players should be in, for recovery after a disconnect or server restart
        private Dictionary<string, Guild> _playerGroups = new Dictionary<string, Guild>();
        private List<GameFlagBasePad> _allBasePads = new List<GameFlagBasePad>();
        private int _flagCounter = 0;
        private RegionTimer _territoryOwnershipTimer = null;

        #region Singleton
        private static PvpManager _instance;
        public static PvpManager Instance => _instance;

        [ScriptLoadedEvent]
        public static void OnServerStart(DOLEvent e, object sender, EventArgs args)
        {
            log.Info("PvpManager: Loading or Starting...");

            if (_instance == null)
                _instance = new PvpManager();

            // Create the timer in region 1 for the open/close checks
            var region = WorldMgr.GetRegion(1);
            if (region != null)
            {
                _timer = new RegionTimer(region.TimeManager);
                _timer.Callback = _instance.TickCheck;
                _timer.Start(10_000); // start after 10s
            }
            else
            {
                log.Warn("PvpManager: Could not find Region(1) for timer!");
            }

            // Load the DB sessions
            PvpSessionMgr.ReloadSessions();
        }

        [ScriptUnloadedEvent]
        public static void OnServerStop(DOLEvent e, object sender, EventArgs args)
        {
            log.Info("PvpManager: Stopping...");
            _timer?.Stop();

            GameEventMgr.RemoveHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(OnPlayerLogin));
        }

        private static void OnPlayerLogin(DOLEvent e, object sender, EventArgs args)
        {
            var player = sender as GamePlayer;
            if (player == null) return;

            if (player.IsInPvP && player.m_PvPGraceTimer != null)
            {
                player.m_PvPGraceTimer.Stop();
                player.m_PvPGraceTimer = null;
                player.Out.SendMessage("Welcome back! Your PvP state has been preserved.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                // Remove any FlagInventoryItem items from the player's backpack
                int totalFlagRemoved = 0;
                for (eInventorySlot slot = eInventorySlot.FirstBackpack;
                     slot <= eInventorySlot.LastBackpack;
                     slot++)
                {
                    InventoryItem item = player.Inventory.GetItem(slot);
                    if (item is FlagInventoryItem flag)
                    {
                        int flagCount = flag.Count;
                        if (player.Inventory.RemoveItem(item))
                        {
                            totalFlagRemoved += flagCount;
                        }
                    }
                }
            }

            // check if we have an RvrPlayer row flagged "PvP"
            RvrPlayer rec = GameServer.Database.SelectObject<RvrPlayer>(DB.Column("PlayerID").IsEqualTo(player.InternalID));
            if (rec == null) return;

            if (rec.SessionType == "PvP")
            {
                if (!Instance.IsOpen || Instance._activeSession == null)
                {
                    Instance.RemovePlayer(player);
                }
                else
                {
                    log.Info($"{player.Name} was flagged SessionType=PvP, forcibly removing from PvP on login if ephemeral guild doesn't exist, etc.");
                    Instance.RemovePlayer(player);
                }
            }
        }

        #endregion

        private PvpManager()
        {
            _isOpen = false;
        }

        #region Timer Check
        private int TickCheck(RegionTimer timer)
        {
            if (!_isOpen)
            {
                if (DateTime.Now.TimeOfDay >= _startTime && DateTime.Now.TimeOfDay < _endTime)
                {
                    Open(string.Empty, false);
                }
            }
            else
            {
                if (!_isForcedOpen && (DateTime.Now.TimeOfDay < _startTime || DateTime.Now.TimeOfDay > _endTime))
                {
                    Close();
                }
            }
            return _checkInterval;
        }
        #endregion

        #region Public Properties
        public bool IsOpen => _isOpen;
        public PvpSession CurrentSession => _activeSession;
        public IReadOnlyList<Zone> CurrentZones => _zones.AsReadOnly();
        public string CurrentSessionId => string.IsNullOrEmpty(CurrentSession?.SessionID) ? "(none)" : CurrentSession.SessionID;
        #endregion

        #region Open/Close

        [return: NotNull]
        private PlayerScore ParsePlayer(string playerId, IEnumerable<string> parameters)
        {
            PlayerScore score = new PlayerScore() { PlayerID = playerId };
            foreach (var playerScoreInfo in parameters)
            {
                var playerScore = playerScoreInfo.Split(':');
                var playerScoreKey = playerScore[0];

                PropertyInfo p = typeof(PlayerScore).GetProperty(playerScoreKey);
                if (p == null)
                {
                    log.Warn($"PvP score \"{playerScoreKey}\" of player {playerId} is unknown");
                    continue;
                }

                int playerScoreValue = 0;
                int.TryParse(playerScore[1], out playerScoreValue);
                p.SetValue(score, playerScoreValue);
            }
            return score; 
        }

        private void ParseGuildEntry(IEnumerator<string> lines, string[] parameters)
        {
            // g;Miuna's guild
            // f1999a9a-f590-453f-ac72-de09afa0c67a=PvP_SoloKills:1=PvP_SoloKillPoints:2
            // c3ceb30f-3441-4fda-abbe-755dc28d9e08
            //
            // g;Bob's guild
            // 2ef3beb7-11fc-4b2a-b2e7-4515275423e0=PvP_SoloKills:1=PvP_SoloKillPoints:2
            var guildName = parameters[1];
            Guild guild = GuildMgr.CreateGuild(eRealm.None, guildName, null, true);
            if (guild == null)
            {
                guild = GuildMgr.GetGuildByName(guildName);
                if (guild == null)
                {
                    log!.Warn($"Cannot recover PvP scores for guild {guildName}, guild could not be found or created");
                }
                return;
            }
            while (lines.MoveNext() && !string.IsNullOrEmpty(lines.Current))
            {
                var values = lines.Current.Split('=');
                var playerId = values[0];
                if (!_playerGroups!.TryAdd(playerId, guild))
                {
                    log!.Warn($"Cannot add player {playerId} to PvP guild {guild.Name}, player is already registered to {_playerGroups[playerId]!.Name}");
                    break;
                }
                GetGroupScore(guild).Scores![playerId] = new GroupScoreEntry(ParsePlayer(playerId, values.Skip(1)), DateTime.Now);
            }
        }

        public bool Open(string sessionID, bool force)
        {
            _isForcedOpen = force;
            if (_isOpen)
                return true;

            _isOpen = true;
            
            // Reset scoreboard, queues, oldInfos
            ResetScores();
            _soloQueue.Clear();
            _groupQueue.Clear();

            // Now we parse the session's ZoneList => find all "SPAWN" NPCs in those zones
            _zones.Clear();
            _spawnNpcsGlobal.Clear();
            _spawnNpcsRealm[eRealm.Albion].Clear();
            _spawnNpcsRealm[eRealm.Midgard].Clear();
            _spawnNpcsRealm[eRealm.Hibernia].Clear();
            _usedSpawns.Clear();
            _soloAreas.Clear();
            _groupAreas.Clear();
            
            if (string.IsNullOrEmpty(sessionID))
            {
                try
                {
                    using var lines = File.ReadLines("temp/PvPScore.dat").GetEnumerator();
                    
                    var header = lines.Current.Split(';');
                    sessionID = header[0];
                    bool.TryParse(header[1], out force);
                    _isForcedOpen = force;

                    bool finished = lines.MoveNext();
                    while (!finished)
                    {
                        var parameters = lines.Current.Split(';');
                        switch (parameters[0])
                        {
                            case "g":
                                {
                                    ParseGuildEntry(lines, parameters);
                                }
                                break;
                            case "p":
                                break;
                        }
                        finished = lines.MoveNext();
                    }
                }
                catch (FileNotFoundException)
                {
                    // fine
                }
                catch (Exception ex)
                {
                    log.Warn("Could not open file temp/PvPScore.dat: ", ex);
                }
            }

            if (string.IsNullOrEmpty(sessionID))
            {
                // pick a random session from DB
                _activeSession = PvpSessionMgr.PickRandomSession();
                if (_activeSession == null)
                {
                    log.Warn("No PvP Sessions in DB, cannot open!");
                    _isOpen = false;
                    return false;
                }
            }
            else
            {
                _activeSession = PvpSessionMgr.GetAllSessions().First(s => string.Equals(s.SessionID, sessionID));
                if (_activeSession == null)
                {
                    log.Warn($"PvP session {sessionID} could not be found, cannot open!");
                    _isOpen = false;
                    return false;
                }
            }

            log.Info($"PvpManager: Opened session [{_activeSession.SessionID}] Type={_activeSession.SessionType}, SpawnOpt={_activeSession.SpawnOption}");

            List<GameNPC> padSpawnNpcsGlobal = new List<GameNPC>();

            var zoneStrings = _activeSession.ZoneList.Split(',');
            foreach (var zStr in zoneStrings)
            {
                if (!ushort.TryParse(zStr.Trim(), out ushort zoneId))
                    continue;

                Zone zone = WorldMgr.GetZone(zoneId);
                if (zone == null) continue;

                _zones.Add(zone);
                var npcs = WorldMgr.GetNPCsByGuild("PVP", eRealm.None).Where(n => n.CurrentZone == zone && n.Name.StartsWith("SPAWN", StringComparison.OrdinalIgnoreCase) &&
                        !n.Name.StartsWith("PADSPAWN", StringComparison.OrdinalIgnoreCase)).ToList();

                _spawnNpcsGlobal.AddRange(npcs);

                // Also see if any are realm-labeled:
                // e.g. "SPAWN-ALB", "SPAWN-MID", "SPAWN-HIB"
                foreach (var npc in npcs)
                {
                    if (npc.Name.IndexOf("SPAWN-ALB", StringComparison.OrdinalIgnoreCase) >= 0)
                        _spawnNpcsRealm[eRealm.Albion].Add(npc);
                    else if (npc.Name.IndexOf("SPAWN-MID", StringComparison.OrdinalIgnoreCase) >= 0)
                        _spawnNpcsRealm[eRealm.Midgard].Add(npc);
                    else if (npc.Name.IndexOf("SPAWN-HIB", StringComparison.OrdinalIgnoreCase) >= 0)
                        _spawnNpcsRealm[eRealm.Hibernia].Add(npc);
                }

                // Retrieve PADSPAWN NPCs separately (used solely for flag pads).
                var padNpcs = WorldMgr.GetNPCsByGuild("PVP", eRealm.None)
                    .Where(n => n.CurrentZone == zone &&
                                n.Name.StartsWith("PADSPAWN", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                padSpawnNpcsGlobal.AddRange(padNpcs);
            }

            // For Flag Capture sessions (SessionType == 2), create a flag pad at each PADSPAWN npc's position.
            if (_activeSession.SessionType == 2)
            {
                foreach (var padSpawnNPC in padSpawnNpcsGlobal)
                {
                    var basePad = new GameFlagBasePad
                    {
                        Position = padSpawnNPC.Position,
                        Model = 2655,
                        Name = "BaseFlagPad",
                        FlagID = ++_flagCounter
                    };

                    basePad.AddToWorld();
                    _allBasePads.Add(basePad);
                }
            }

            if (_activeSession.SessionType == 4)
            {
                GameEventMgr.AddHandler(GameLivingEvent.Dying, new DOLEventHandler(OnLivingDying_BringAFriend));
                GameEventMgr.AddHandler(GameLivingEvent.BringAFriend, new DOLEventHandler(OnBringAFriend));
            }
            
            // If we found zero spawns, the fallback logic in FindSpawnPosition might do random coords.
            // Or you can log a warning:
            if (_spawnNpcsGlobal.Count == 0)
            {
                log.Warn("No 'SPAWN' NPCs found in the session's zones. We'll fallback to random coords.");
            }

            if (_activeSession.SessionType == 5)
            {
                var reg = WorldMgr.GetRegion(1);
                if (reg != null)
                {
                    _territoryOwnershipTimer = new RegionTimer(reg.TimeManager);
                    _territoryOwnershipTimer.Callback = AwardTerritoryOwnershipPoints;
                    _territoryOwnershipTimer.Interval = 20_000;
                    _territoryOwnershipTimer.Start(10_000);
                }
            }

            return true;
        }

        public bool Close()
        {
            if (!_isOpen)
                return false;

            _isOpen = false;
            _isForcedOpen = false;

            log.InfoFormat("PvpManager: Closing session [{0}].", _activeSession?.SessionID);

            if (_activeSession != null && _activeSession.SessionType == 5 && _territoryOwnershipTimer != null)
            {
                _territoryOwnershipTimer.Stop();
                _territoryOwnershipTimer = null;
            }

            // Force remove all players still flagged IsInPvP
            foreach (var client in WorldMgr.GetAllPlayingClients())
            {
                var plr = client?.Player;
                if (plr != null && plr.IsInPvP)
                {
                    RemovePlayer(plr);
                }
            }

            foreach (var pad in _allBasePads)
            {
                pad.RemoveFlag();
                pad.RemoveFromWorld();
            }
            _allBasePads.Clear();

            // remove all solo areas
            foreach (var kv in _soloAreas)
            {
                var area = kv.Value;
                if (area != null)
                {
                    var region = kv.Key.CurrentRegion;
                    region?.RemoveArea(area);
                }
            }
            _soloAreas.Clear();

            // remove all group areas
            foreach (var kv in _groupAreas)
            {
                var area = kv.Value;
                if (area != null)
                {
                    var leader = kv.Key.Leader as GamePlayer;
                    leader?.CurrentRegion?.RemoveArea(area);
                }
            }
            _groupAreas.Clear();

            if (_activeSession != null && _activeSession.SessionType == 4)
            {
                GameEventMgr.RemoveHandler(GameLivingEvent.Dying, new DOLEventHandler(OnLivingDying_BringAFriend));
                GameEventMgr.RemoveHandler(GameLivingEvent.BringAFriend, new DOLEventHandler(OnBringAFriend));

                // For each zone in this session, find all FollowingFriendMob and Reset them
                var zones = _activeSession.ZoneList.Split(',');
                foreach (var zStr in zones)
                {
                    if (!ushort.TryParse(zStr, out ushort zoneId)) continue;
                    var z = WorldMgr.GetZone(zoneId);
                    if (z == null) continue;

                    var allNpcs = WorldMgr.GetNPCsFromRegion(z.ZoneRegion.ID).Where(n => n.CurrentZone == z);
                    foreach (var npc in allNpcs)
                    {
                        if (npc is FollowingFriendMob ff)
                        {
                            ff.ResetFollow();
                        }
                    }
                }
            }

            if (_activeSession != null && _activeSession.SessionType == 5)
            {
                TerritoryManager.Instance.ReleaseSubTerritoriesInZones(CurrentZones);
            }

            ResetScores();
            _activeSession = null;
            _soloQueue.Clear();
            _groupQueue.Clear();

            return true;
        }

        public AbstractArea FindSafeAreaForTarget(GamePlayer player)
        {
            if (player == null) return null;

            // If solo
            if (player.Group == null || player.Group.MemberCount <= 1)
            {
                if (_soloAreas.TryGetValue(player, out var soloArea))
                    return soloArea;
                return null;
            }
            else
            {
                // Group scenario
                if (player.Group != null && _groupAreas.TryGetValue(player.Group, out var groupArea))
                {
                    return groupArea;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns true if this player is in one of the zone(s) configured by the active session.
        /// </summary>
        public bool IsInActivePvpZone(GamePlayer player)
        {
            if (!_isOpen || _activeSession == null) return false;
            if (player == null || player.CurrentZone == null) return false;

            ushort zoneID = player.CurrentZone.ID;
            // parse the zone IDs from _activeSession.ZoneList
            var zoneStrs = _activeSession.ZoneList.Split(',');
            foreach (var zStr in zoneStrs)
            {
                if (ushort.TryParse(zStr, out ushort zId))
                {
                    if (zId == zoneID)
                        return true;
                }
            }
            return false;
        }
        #endregion

        #region Scoreboard
        /// <summary>
        /// Clear the _playerScores dictionary.
        /// </summary>
        private void ResetScores()
        {
            _playerScores.Clear();
            _groupScores.Clear();
        }

        [return: NotNullIfNotNull(nameof(guild))]
        public GroupScore GetGroupScore(Guild? guild)
        {
            if (guild == null)
                return null;

            if (!_groupScores.TryGetValue(guild, out GroupScore score))
            {
                score = new GroupScore(
                    guild,
                    new PlayerScore() { PlayerID = guild.GuildID, PlayerName = guild.Name },
                    new()
                );
                _groupScores[guild] = score;
            }
            return score;
        }

        public (GroupScore score, GroupScoreEntry entry) GetGroupScoreEntry(GamePlayer player)
        {
            if (player?.Guild == null)
                return (null, null);

            GroupScoreEntry entry;
            if (!_groupScores.TryGetValue(player.Guild, out GroupScore score))
            {
                entry = new GroupScoreEntry(new PlayerScore
                {
                    PlayerID = player.InternalID,
                    PlayerName = player.Name
                }, DateTime.Now);
                score = new GroupScore(
                    player.Guild,
                    new PlayerScore() { PlayerID = player.Guild.GuildID, PlayerName = player.Guild.Name},
                    new Dictionary<string, GroupScoreEntry>
                    {
                        { player.InternalID, entry }
                    }
                );
                _groupScores[player.Guild] = score;
            }
            else if (!score.Scores.TryGetValue(player.InternalID, out entry))
            {
                entry = new GroupScoreEntry(new PlayerScore
                {
                    PlayerID = player.InternalID,
                    PlayerName = player.Name
                }, DateTime.Now);
                score.Scores[player.InternalID] = entry;
            }
            return (score, entry);
        }
        
        [return: NotNullIfNotNull(nameof(player))]
        public PlayerScore GetScoreRecord(GamePlayer player)
        {
            if (player == null)
                return null;

            string pid = player.InternalID;
            if (!_playerScores.TryGetValue(pid, out PlayerScore score))
            {
                score = new PlayerScore()
                {
                    PlayerID = pid,
                    PlayerName = player.Name
                };
                _playerScores[pid] = score;
            }
            return score;
        }

        public void HandlePlayerKill(GamePlayer killer, GamePlayer victim)
        {
            if (!_isOpen || _activeSession == null) return;

            if (!killer.IsInPvP || !victim.IsInPvP) return;

            switch (_activeSession.SessionType)
            {
                case 1:
                    UpdateScores_PvPCombat(killer, victim);
                    break;
                case 2:
                    UpdateScores_FlagCapture(killer, victim, wasFlagCarrier: false);
                    break;
                case 3:
                    UpdateScores_TreasureHunt(killer, victim);
                    break;
                case 4:
                    UpdateScores_BringFriends(killer, victim);
                    break;
                case 5:
                    UpdateScores_CaptureTerritories(killer, victim);
                    break;
                case 6:
                    UpdateScores_BossKillCoop(killer, victim);
                    break;
                default:
                    break;
            }
        }

        private bool IsSolo(GamePlayer killer)
        {
            return (killer.Group == null || killer.Group.MemberCount <= 1);
        }

        /// <summary>
        /// "PvP Combat" (SessionType=1).
        /// Scoring rules:
        /// - Solo kill => 10 pts (+30% if victim is RR5+)
        /// - Group kill => 5 pts (+30% if victim is RR5+)
        /// </summary>
        private void UpdateScores_PvPCombat(GamePlayer killer, GamePlayer victim)
        {
            var killerScore = GetScoreRecord(killer);
            if (killerScore == null) return;

            // check if victim is RR5 or more
            bool rr5bonus = (victim.RealmLevel >= 40);
            bool isSolo = (killer.Group == null || killer.Group.MemberCount <= 1);
            var (groupScore, groupEntry) = GetGroupScoreEntry(killer);
            var fun = (PlayerScore score) =>
            {
                if (isSolo)
                {
                    score.PvP_SoloKills++;
                    int basePts = 10;
                    if (rr5bonus) basePts = (int)(basePts * 1.30);
                    score.PvP_SoloKillsPoints += basePts;
                }
                else
                {
                    score.PvP_GroupKills++;
                    int basePts = 5;
                    if (rr5bonus) basePts = (int)(basePts * 1.30);
                    score.PvP_GroupKillsPoints += basePts;
                }
            };
            fun(killerScore);
            if (groupScore != null)
            {
                fun(groupEntry.PlayerScore);
                fun(groupScore.Totals);
            }
        }

        /// <summary>
        /// "Flag Capture" (SessionType=2).
        /// In reality, you'd also handle separate events for "bring flag to outpost",
        /// "flag ownership tick", etc...
        /// + special points if the victim was carrying the flag.
        /// </summary>
        public void UpdateScores_FlagCapture(GamePlayer killer, GamePlayer victim, bool wasFlagCarrier)
        {
            var killerScore = GetScoreRecord(killer);
            if (killerScore == null) return;

            bool isSolo = IsSolo(killer);
            var (groupScore, groupEntry) = GetGroupScoreEntry(killer);
            var fun = (PlayerScore score) =>
            {
                if (!wasFlagCarrier)
                {
                    if (isSolo)
                    {
                        score.Flag_SoloKills++;
                        score.Flag_SoloKillsPoints += 4;
                    }
                    else
                    {
                        score.Flag_GroupKills++;
                        score.Flag_GroupKillsPoints += 2;
                    }
                }
                else
                {
                    score.Flag_KillFlagCarrierCount++;
                    score.Flag_KillFlagCarrierPoints += 6;
                }
            };
            fun(killerScore);
            if (groupScore != null)
            {
                fun(groupEntry.PlayerScore);
                fun(groupScore.Totals);
            }
        }
        
        /// <summary>
        /// "Treasure Hunt" (SessionType=3).
        /// </summary>
        private void UpdateScores_TreasureHunt(GamePlayer killer, GamePlayer victim)
        {
            var killerScore = GetScoreRecord(killer);
            if (killerScore == null) return;

            bool isSolo = IsSolo(killer);
            var (groupScore, groupEntry) = GetGroupScoreEntry(killer);
            var fun = (PlayerScore score) =>
            {
                if (isSolo)
                {
                    score.Treasure_SoloKills++;
                    score.Treasure_SoloKillsPoints += 4;
                }
                else
                {
                    score.Treasure_GroupKills++;
                    score.Treasure_GroupKillsPoints += 2;
                }
            };
            fun(killerScore);
            if (groupScore != null)
            {
                fun(groupEntry.PlayerScore);
                fun(groupScore.Totals);
            }
        }

        /// <summary>
        /// "Bring Friends" (SessionType=4).
        /// </summary>
        private void UpdateScores_BringFriends(GamePlayer killer, GamePlayer victim)
        {
            var killerScore = GetScoreRecord(killer);
            if (killerScore == null) return;

            bool isSolo = IsSolo(killer);
            var (groupScore, groupEntry) = GetGroupScoreEntry(killer);
            var fun = (PlayerScore score) =>
            {
                if (isSolo)
                {
                    score.Friends_SoloKills++;
                    score.Friends_SoloKillsPoints += 4;
                }
                else
                {
                    score.Friends_GroupKills++;
                    score.Friends_GroupKillsPoints += 2;
                }
            };
            fun(killerScore);
            if (groupScore != null)
            {
                fun(groupEntry.PlayerScore);
                fun(groupScore.Totals);
            }
        }

        /// <summary>
        /// "Capture Territories" (SessionType=5).
        /// </summary>
        private void UpdateScores_CaptureTerritories(GamePlayer killer, GamePlayer victim)
        {
            var killerScore = GetScoreRecord(killer);
            if (killerScore == null) return;

            bool isSolo = IsSolo(killer);
            var (groupScore, groupEntry) = GetGroupScoreEntry(killer);
            var fun = (PlayerScore score) =>
            {
                if (isSolo)
                {
                    score.Terr_SoloKills++;
                    score.Terr_SoloKillsPoints += 4;
                }
                else
                {
                    score.Terr_GroupKills++;
                    score.Terr_GroupKillsPoints += 2;
                }
            };
            fun(killerScore);
            if (groupScore != null)
            {
                fun(groupEntry.PlayerScore);
                fun(groupScore.Totals);
            }
        }

        /// <summary>
        /// "Boss Kill Cooperation" (SessionType=6).
        /// </summary>
        private void UpdateScores_BossKillCoop(GamePlayer killer, GamePlayer victim)
        {
            var killerScore = GetScoreRecord(killer);
            if (killerScore == null) return;

            // check if victim is RR5 or more
            bool rr5bonus = (victim.RealmLevel >= 40);

            bool isSolo = IsSolo(killer);
            var (groupScore, groupEntry) = GetGroupScoreEntry(killer);
            var fun = (PlayerScore score) =>
            {
                if (isSolo)
                {
                    score.Boss_SoloKills++;
                    int basePts = 30;
                    if (rr5bonus) basePts = (int)(basePts * 1.30);
                    score.Boss_SoloKillsPoints += basePts;
                }
                else
                {
                    score.Boss_GroupKills++;
                    int basePts = 15;
                    if (rr5bonus) basePts = (int)(basePts * 1.30);
                    score.Boss_GroupKillsPoints += basePts;
                }
            };
            fun(killerScore);
            if (groupScore != null)
            {
                fun(groupEntry.PlayerScore);
                fun(groupScore.Totals);
            }
        }
        #endregion

        #region Add Player/Group (with Guild + Bind Logic)
        /// <summary>
        /// Add a single player (solo) to PvP.
        /// Called by Teleporter or other code.
        /// </summary>
        public bool AddPlayer(GamePlayer player)
        {
            if (!_isOpen || _activeSession == null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PvPManager.PvPNotOpen"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            // If group-only session => forbid
            if (_activeSession.GroupCompoOption == 2)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PvPManager.GroupSessionRequired"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            // Optionally forbid GMs
            // if (player.Client.Account.PrivLevel == (uint)ePrivLevel.GM)
            // {
            //     player.Out.SendMessage("GM not allowed in PvP!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            //     return false;
            // }

            // Store old info, remove guild
            StoreOldPlayerInfo(player);

            TeleportSoloPlayer(player);
            player.Bind(true);
            player.SaveIntoDatabase();

            return true;
        }

        /// <summary>
        /// Add an entire group. Called by Teleporter or other code.
        /// </summary>
        public bool AddGroup(GamePlayer groupLeader)
        {
            if (!_isOpen || _activeSession == null) return false;

            var group = groupLeader.Group;
            if (group == null)
            {
                groupLeader.Out.SendMessage(LanguageMgr.GetTranslation(groupLeader.Client.Account.Language, "PvPManager.NoGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (group.MemberCount > _activeSession.GroupMaxSize)
            {
                groupLeader.Out.SendMessage(LanguageMgr.GetTranslation(groupLeader.Client.Account.Language, "PvPManager.GroupTooLarge", _activeSession.GroupMaxSize), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            foreach (var member in group.GetPlayersInTheGroup())
            {
                StoreOldPlayerInfo(member);
            }

            // CREATE or GET the ephemeral guild for this group
            if (!_groupGuilds.TryGetValue(group, out Guild pvpGuild))
            {
                string guildName = groupLeader.Name + "'s guild";
                pvpGuild = GuildMgr.CreateGuild(eRealm.None, guildName, groupLeader, true);

                if (pvpGuild == null)
                {
                    groupLeader.Out.SendMessage(LanguageMgr.GetTranslation(groupLeader.Client.Account.Language, "PvPManager.CannotCreatePvPGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                pvpGuild.GuildType = Guild.eGuildType.PvPGuild;
                _groupGuilds[group] = pvpGuild;

                int[] emblemChoices = new int[] { 5061, 6645, 84471, 6272, 55302, 64792, 111402, 39859, 21509, 123019 };
                pvpGuild.Emblem = emblemChoices[Util.Random(emblemChoices.Length - 1)];
                pvpGuild.SaveIntoDatabase();
            }

            // Add each member to the ephemeral guild
            foreach (var member in group.GetPlayersInTheGroup())
            {
                if (member == groupLeader)
                    pvpGuild.AddPlayer(member, pvpGuild.GetRankByID(0));
                else
                    pvpGuild.AddPlayer(member, pvpGuild.GetRankByID(9));

                member.IsInPvP = true;
                member.Bind(true);
                member.SaveIntoDatabase();
            }

            TeleportEntireGroup(groupLeader);
            return true;
        }

        /// <summary>
        /// Remove a single player from PvP, restoring them to old location + old guild, etc.
        /// </summary>
        public void RemovePlayer(GamePlayer player)
        {
            if (!player.IsInPvP)
                return;

            RvrPlayer record = GameServer.Database.SelectObject<RvrPlayer>(DB.Column("PlayerID").IsEqualTo(player.InternalID));
            if (record != null && record.SessionType == "PvP")
            {
                RemovePlayerDB(player, record);
            }
            else
            {
                // Fallback: move the player to a safe location.
                var fallbackPos = Position.Create(51, 434303, 493165, 3088, 1069);
                player.MoveTo(fallbackPos);
                player.IsInPvP = false;
            }
        }

        private void RemovePlayerDB(GamePlayer player, RvrPlayer record)
        {
            Group g = player.Group;
            if (g != null && _groupGuilds.TryGetValue(g, out Guild pvpGuild))
            {
                if (player.Guild == pvpGuild)
                {
                    pvpGuild.RemovePlayer("PVP", player);
                }
            }

            int totalRemoved = 0;
            var slotsToCheck = new List<eInventorySlot>();
            for (eInventorySlot slot = eInventorySlot.FirstBackpack;
                 slot <= eInventorySlot.LastBackpack;
                 slot++)
            {
                slotsToCheck.Add(slot);
            }

            foreach (var slot in slotsToCheck)
            {
                InventoryItem item = player.Inventory.GetItem(slot);
                if (item is PvPTreasure treasure)
                {
                    int count = treasure.Count;
                    if (player.Inventory.RemoveItem(item))
                    {
                        totalRemoved += count;
                    }
                }
                else if (item is FlagInventoryItem flag)
                {
                    int flagcount = flag.Count;
                    if (player.Inventory.RemoveItem(item))
                    {
                        totalRemoved += flagcount;
                    }
                }
            }

            if (totalRemoved > 0)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PvPManager.PvPTreasureRemoved", totalRemoved), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }

            record.ResetCharacter(player);
            player.MoveTo(Position.Create((ushort)record.OldRegion, record.OldX, record.OldY, record.OldZ, (ushort)record.OldHeading));

            // If the player was in a guild before PvP, re-add them.
            if (!string.IsNullOrEmpty(record.GuildID))
            {
                var oldGuild = GuildMgr.GetGuildByGuildID(record.GuildID);
                if (oldGuild != null)
                {
                    oldGuild.AddPlayer(player, oldGuild.GetRankByID(record.GuildRank), true);
                }
            }
            player.IsInPvP = false;

            GameServer.Database.DeleteObject(record);

            // Remove any ephemeral PvP guild if no member in that group remains in PvP.
            if (g != null && _groupGuilds.TryGetValue(g, out Guild TempPvpGuild))
            {
                if (!g.GetPlayersInTheGroup().Any(m => m.IsInPvP))
                {
                    GuildMgr.DeleteGuild(TempPvpGuild.Name);
                    _groupGuilds.Remove(g);
                }
            }

            DequeueSolo(player);
            if (_soloAreas.TryGetValue(player, out var area))
            {
                if (area is PvpCircleArea circle)
                    circle.RemoveAllOwnedObjects();
                else if (area is PvpSafeArea pvpArea)
                    pvpArea.RemoveAllOwnedObjects();

                player.CurrentRegion?.RemoveArea(area);
                _soloAreas.Remove(player);
            }
            if (g != null && _groupAreas.TryGetValue(g, out var grpArea))
            {
                // Check if *all* members have left. If so, remove area:
                if (!g.GetPlayersInTheGroup().Any(m => m.IsInPvP))
                {
                    if (grpArea is PvpCircleArea circle)
                        circle.RemoveAllOwnedObjects();
                    else if (grpArea is PvpSafeArea pvpGroupArea)
                        pvpGroupArea.RemoveAllOwnedObjects();

                    g.Leader?.CurrentRegion?.RemoveArea(grpArea);
                    _groupAreas.Remove(g);
                }
            }

            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PvPManager.LeftPvP"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public void RemovePlayerForQuit(GamePlayer player)
        {
            int totalRemoved = 0;
            for (eInventorySlot slot = eInventorySlot.FirstBackpack; slot <= eInventorySlot.LastBackpack; slot++)
            {
                var item = player.Inventory.GetItem(slot);
                if (item is FlagInventoryItem || item is PvPTreasure)
                {
                    int count = item.Count;
                    if (player.Inventory.RemoveItem(item))
                        totalRemoved += count;
                }
            }

            RvrPlayer record = GameServer.Database.SelectObject<RvrPlayer>(DB.Column("PlayerID").IsEqualTo(player.InternalID));
            if (record != null && record.SessionType == "PvP")
            {
                GameServer.Database.DeleteObject(record);
            }

            var g = player.Group;
            if (g != null && _groupGuilds.TryGetValue(g, out Guild pvpGuild))
            {
                if (player.Guild == pvpGuild)
                {
                    pvpGuild.RemovePlayer("PVP", player);
                }

                bool stillHasMemberInPvP = g.GetPlayersInTheGroup().Any(m => m.IsInPvP);
                if (!stillHasMemberInPvP)
                {
                    GuildMgr.DeleteGuild(pvpGuild.Name);
                    _groupGuilds.Remove(g);
                }
            }

            DequeueSolo(player);
            if (_soloAreas.TryGetValue(player, out var soloArea))
            {
                if (soloArea is PvpCircleArea circle)
                    circle.RemoveAllOwnedObjects();
                else if (soloArea is PvpSafeArea pvpArea)
                    pvpArea.RemoveAllOwnedObjects();

                player.CurrentRegion?.RemoveArea(soloArea);
                _soloAreas.Remove(player);
            }
            if (g != null && _groupAreas.TryGetValue(g, out var grpArea))
            {
                bool anyoneStillPvP = g.GetPlayersInTheGroup().Any(m => m.IsInPvP);
                if (!anyoneStillPvP)
                {
                    if (grpArea is PvpCircleArea circle)
                        circle.RemoveAllOwnedObjects();
                    else if (grpArea is PvpSafeArea pvpGroupArea)
                        pvpGroupArea.RemoveAllOwnedObjects();

                    g.Leader?.CurrentRegion?.RemoveArea(grpArea);
                    _groupAreas.Remove(g);
                }
            }

            player.IsInPvP = false;
        }

        /// <summary>
        /// Store player's old location, guild, and optional bind.
        /// Remove them from their current guild if any.
        /// </summary>
        private void StoreOldPlayerInfo(GamePlayer player)
        {
            RvrPlayer record = GameServer.Database.SelectObject<RvrPlayer>(DB.Column("PlayerID").IsEqualTo(player.InternalID));
            bool isNew = false;
            if (record == null)
            {
                record = new RvrPlayer(player);
                isNew = true;
            }
            else
            {
                record.PlayerID = player.InternalID;
                record.GuildID = player.GuildID ?? "";
                record.GuildRank = (player.GuildRank != null) ? player.GuildRank.RankLevel : 9;
                record.OldX = (int)player.Position.X;
                record.OldY = (int)player.Position.Y;
                record.OldZ = (int)player.Position.Z;
                record.OldHeading = player.Heading;
                record.OldRegion = player.CurrentRegionID;
                record.OldBindX = player.BindPosition.Coordinate.X;
                record.OldBindY = player.BindPosition.Coordinate.Y;
                record.OldBindZ = player.BindPosition.Coordinate.Z;
                record.OldBindHeading = (int)player.BindPosition.Orientation.InHeading;
                record.OldBindRegion = player.BindPosition.RegionID;
            }

            record.SessionType = "PvP";

            if (isNew)
                GameServer.Database.AddObject(record);
            else
                GameServer.Database.SaveObject(record);

            if (player.Guild != null)
                player.Guild.RemovePlayer("PVP", player);

            player.IsInPvP = true;
        }
        #endregion

        #region Teleport logic
        private void TeleportSoloPlayer(GamePlayer player)
        {
            if (!player.IsInPvP)
                player.IsInPvP = true;

            Position? spawnPos = FindSpawnPosition(player);
            if (!spawnPos.HasValue)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PvPManager.CannotFindSpawn"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            player.MoveTo(spawnPos.Value);

            // if session says create area + randomlock => do it
            if (_activeSession.CreateCustomArea &&
                _activeSession.SpawnOption.Equals("RandomLock", StringComparison.OrdinalIgnoreCase))
            {
                CreateSafeAreaForSolo(player, spawnPos.Value, _activeSession.TempAreaRadius);
            }

            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PvPManager.JoinedPvP", _activeSession.SessionID), eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        private void TeleportEntireGroup(GamePlayer leader)
        {
            var group = leader.Group;
            if (group == null) return;

            Position? spawnPosLeader = null;

            foreach (var member in group.GetPlayersInTheGroup())
            {
                if (!member.IsInPvP)
                    member.IsInPvP = true;

                var spawnPos = FindSpawnPosition(member);
                if (spawnPos.HasValue)
                {
                    // store the leader's spawn position if we want to place the safe area exactly for him
                    if (member == leader)
                        spawnPosLeader = spawnPos;

                    member.MoveTo(spawnPos.Value);
                }
            }

            // Only create one safe area for the entire group (use leader's position)
            if (spawnPosLeader.HasValue &&
                _activeSession.CreateCustomArea &&
                _activeSession.SpawnOption.Equals("RandomLock", StringComparison.OrdinalIgnoreCase))
            {
                CreateSafeAreaForGroup(leader, spawnPosLeader.Value, _activeSession.TempAreaRadius);
            }

            leader.Out.SendMessage(LanguageMgr.GetTranslation(leader.Client.Account.Language, "PvPManager.GroupJoinedPvP", _activeSession.SessionID), eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        private Position? FindSpawnPosition(GamePlayer player)
        {
            // 1) If we have no session or no spawns loaded => fallback
            if (_activeSession == null)
                return null;

            // Decide spawn option ( "RealmSpawn", "RandomLock", "RandomUnlock", etc. )
            string spawnOpt = _activeSession.SpawnOption.ToLowerInvariant();

            // 2) If "RealmSpawn" => pick from the realm-labeled spawns
            if (spawnOpt == "realmspawn")
            {
                var realmSpawns = _spawnNpcsRealm[player.Realm];
                if (realmSpawns != null && realmSpawns.Count > 0)
                {
                    // pick any spawn from that list at random
                    int idx = Util.Random(realmSpawns.Count - 1);
                    var chosenSpawn = realmSpawns[idx];
                    return chosenSpawn.Position;
                }
                else
                {
                    log.Warn($"RealmSpawn: no spawns for realm {player.Realm}, falling back to random coords.");
                }
            }

            // 3) If "RandomLock" => pick from the global spawns, skipping used ones
            if (spawnOpt == "randomlock")
            {
                var available = _spawnNpcsGlobal.Where(n => !_usedSpawns.Contains(n)).ToList();
                if (available.Count > 0)
                {
                    var chosen = available[Util.Random(available.Count - 1)];
                    _usedSpawns.Add(chosen);
                    return chosen.Position;
                }
                else
                {
                    log.Warn("RandomLock: all spawns used. Fallback to random coords.");
                }
            }

            // 4) If "RandomUnlock" => pick from entire _spawnNpcsGlobal randomly
            // (or if spawnOpt is some other unknown string, we default to random unlock)
            {
                if (_spawnNpcsGlobal.Count > 0)
                {
                    var chosen = _spawnNpcsGlobal[Util.Random(_spawnNpcsGlobal.Count - 1)];
                    return chosen.Position;
                }
                else
                {
                    log.Warn("RandomUnlock: no spawn NPC found, fallback random coords.");
                }
            }

            // 5) Fallback: if we got here, it means no spawn NPC found or spawnOpt is unknown => random
            // parse the session's zone list => pick first zone => random coordinate
            // (this is basically your old approach)

            var zoneStrings = _activeSession.ZoneList.Split(',');
            if (zoneStrings.Length == 0)
                return null;

            if (!ushort.TryParse(zoneStrings[0], out ushort zoneId))
                return null;

            Zone zone = WorldMgr.GetZone(zoneId);
            if (zone == null)
                return null;

            int xcoord = zone.Offset.X + 3000 + Util.Random(1000);
            int ycoord = zone.Offset.Y + 3000 + Util.Random(1000);
            int zcoord = 3000;
            ushort heading = 2048;

            return Position.Create(zone.ZoneRegion.ID, xcoord, ycoord, zcoord, heading);
        }
        #endregion

        #region Queue logic
        public void EnqueueSolo(GamePlayer player)
        {
            if (_soloQueue.Contains(player))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PvPManager.AlreadyInSoloQueue"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            _soloQueue.Add(player);
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PvPManager.JoinedSoloQueue"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

            TryFormGroupFromSoloQueue();
        }

        private void TryFormGroupFromSoloQueue()
        {
            if (_activeSession == null)
                return;

            int needed = _activeSession.GroupMaxSize;
            if (needed < 2) needed = 2;

            while (_soloQueue.Count >= needed)
            {
                var groupPlayers = _soloQueue.Take(needed).ToList();
                _soloQueue.RemoveRange(0, needed);

                // forcibly create a new DOL Group
                var randomLeader = groupPlayers[Util.Random(groupPlayers.Count - 1)];
                var newGroup = new Group(randomLeader);
                GroupMgr.AddGroup(newGroup);
                newGroup.AddMember(randomLeader);

                foreach (var p in groupPlayers)
                {
                    if (p == randomLeader) continue;
                    newGroup.AddMember(p);
                }

                // for each player, store old info, remove guild
                foreach (var p in groupPlayers)
                {
                    StoreOldPlayerInfo(p);
                }

                string guildName = randomLeader.Name + "'s guild";
                var pvpGuild = GuildMgr.CreateGuild(eRealm.None, guildName, randomLeader);

                if (pvpGuild == null)
                {
                    randomLeader.Out.SendMessage(LanguageMgr.GetTranslation(randomLeader.Client.Account.Language, "PvPManager.CouldNotCreatePvPGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    continue;
                }

                pvpGuild.GuildType = Guild.eGuildType.PvPGuild;
                _groupGuilds[newGroup] = pvpGuild;

                int[] emblemChoices = new int[] { 5061, 6645, 84471, 6272, 55302, 64792, 111402, 39859, 21509, 123019 };
                pvpGuild.Emblem = emblemChoices[Util.Random(emblemChoices.Length - 1)];
                pvpGuild.SaveIntoDatabase();

                foreach (var p in groupPlayers)
                {
                    if (p == randomLeader)
                        pvpGuild.AddPlayer(p, pvpGuild.GetRankByID(0));
                    else
                        pvpGuild.AddPlayer(p, pvpGuild.GetRankByID(9));

                    p.IsInPvP = true;
                    p.Bind(true);
                    p.SaveIntoDatabase();
                }

                TeleportEntireGroup(randomLeader);
            }
        }

        public void DequeueSolo(GamePlayer player)
        {
            if (_soloQueue.Remove(player))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PvPManager.LeftGroupQueue"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        public bool IsPlayerInQueue(GamePlayer player)
        {
            return _soloQueue.Contains(player);
        }
        #endregion

        #region Safe Area Creation
        private void CreateSafeAreaForSolo(GamePlayer player, Position pos, int radius)
        {
            bool isBringFriends = _activeSession.SessionType == 4;
            string areaName = player.Name + "'s Solo Outpost";

            AbstractArea areaObject;

            if (!isBringFriends)
            {
                var circleArea = new PvpCircleArea(areaName, pos.X, pos.Y, pos.Z, radius)
                {
                    OwnerPlayer = player,
                    OwnerGuild = null
                };
                areaObject = circleArea;
            }
            else
            {
                var safeArea = new PvpSafeArea(areaName, pos.X, pos.Y, pos.Z, radius)
                {
                    OwnerPlayer = player,
                    OwnerGuild = null
                };
                areaObject = safeArea;
            }

            player.CurrentRegion.AddArea(areaObject);
            _soloAreas[player] = areaObject;

            log.Info("PvpManager: Created a solo outpost for " + player.Name);

            if (_activeSession.SessionType == 2)
            {
                var outpostPadItems = PvPAreaOutposts.CreateCaptureFlagOutpostPad(pos, player, null);

                foreach (var staticItem in outpostPadItems)
                {
                    if (areaObject is PvpCircleArea circle)
                        circle.AddOwnedObject(staticItem);
                    else if (areaObject is PvpSafeArea safeArea)
                        safeArea.AddOwnedObject(staticItem);

                    if (staticItem is GameCTFTempPad temppad)
                    {
                        temppad.SetOwnerSolo(player);
                    }
                }
            }

            if (_activeSession.SessionType == 3)
            {
                var outpostItems = PvPAreaOutposts.CreateTreasureHuntBase(pos, player, null);

                foreach (var staticItem in outpostItems)
                {
                    if (areaObject is PvpCircleArea circle)
                        circle.AddOwnedObject(staticItem);
                    else if (areaObject is PvpSafeArea safeArea)
                        safeArea.AddOwnedObject(staticItem);

                    if (staticItem is PVPChest chest)
                    {
                        chest.SetOwnerSolo(player);
                    }
                }
            }

            if (_activeSession.SessionType == 5 || _activeSession.SessionType == 6)
            {
                var outpostItems = PvPAreaOutposts.CreateGuildOutpostTemplate01(pos, player, null);
                foreach (var staticItem in outpostItems)
                {
                    if (areaObject is PvpCircleArea circle)
                        circle.AddOwnedObject(staticItem);
                    else if (areaObject is PvpSafeArea safeArea)
                        safeArea.AddOwnedObject(staticItem);
                    // (Optionally, set additional ownership if needed)
                }
            }
        }

        private void CreateSafeAreaForGroup(GamePlayer leader, Position pos, int radius)
        {
            if (leader.Group == null) return;

            bool isBringFriends = _activeSession.SessionType == 4;
            string areaName = leader.Name + "'s Group Outpost";
            // Retrieve the ephemeral guild you made for the group
            var group = leader.Group;
            _groupGuilds.TryGetValue(group, out Guild pvpGuild);

            AbstractArea areaObject;

            if (!isBringFriends)
            {
                var circleArea = new PvpCircleArea(areaName, pos.X, pos.Y, pos.Z, radius)
                {
                    OwnerGuild = pvpGuild,
                    OwnerPlayer = null
                };
                areaObject = circleArea;
            }
            else
            {
                var safeArea = new PvpSafeArea(areaName, pos.X, pos.Y, pos.Z, radius)
                {
                    OwnerGuild = pvpGuild,
                    OwnerPlayer = null
                };
                areaObject = safeArea;
            }

            leader.CurrentRegion.AddArea(areaObject);
            _groupAreas[group] = areaObject;

            log.Info("PvpManager: Created a group outpost for " + leader.Name);

            if (_activeSession.SessionType == 2)
            {
                var outpostPadItems = PvPAreaOutposts.CreateCaptureFlagOutpostPad(pos, leader, null);

                foreach (var staticItem in outpostPadItems)
                {
                    if (areaObject is PvpCircleArea circle)
                        circle.AddOwnedObject(staticItem);
                    else if (areaObject is PvpSafeArea safeArea)
                        safeArea.AddOwnedObject(staticItem);

                    if (staticItem is GameCTFTempPad temppad)
                    {
                        temppad.SetOwnerGroup(leader, leader.Group);
                    }
                }
            }

            if (_activeSession.SessionType == 3)
            {
                var outpostItems = PvPAreaOutposts.CreateTreasureHuntBase(pos, leader, null);

                foreach (var staticItem in outpostItems)
                {
                    if (areaObject is PvpCircleArea circle)
                        circle.AddOwnedObject(staticItem);
                    else if (areaObject is PvpSafeArea safeArea)
                        safeArea.AddOwnedObject(staticItem);

                    if (staticItem is PVPChest chest)
                    {
                        chest.SetOwnerGroup(leader, leader.Group);
                    }
                }
            }

            if (_activeSession.SessionType == 5 || _activeSession.SessionType == 6)
            {
                var outpostItems = PvPAreaOutposts.CreateGuildOutpostTemplate01(pos, leader, null);
                foreach (var staticItem in outpostItems)
                {
                    if (areaObject is PvpCircleArea circle)
                        circle.AddOwnedObject(staticItem);
                    else if (areaObject is PvpSafeArea safeArea)
                        safeArea.AddOwnedObject(staticItem);
                    // (Optionally, set additional ownership if needed)
                }
            }
        }
        #endregion

        #region BringAFriend Methods & Scores
        private void OnBringAFriend(DOLEvent e, object sender, EventArgs args)
        {
            if (!_isOpen || _activeSession == null || _activeSession.SessionType != 4)
                return;

            var living = sender as GameLiving;
            if (living == null) return;

            var baArgs = args as BringAFriendArgs;
            if (baArgs == null) return;
            if (!(baArgs.Friend is FollowingFriendMob friendMob)) return;

            if (baArgs.Entered && baArgs.FinalStage)
            {
                var player = living as GamePlayer;
                if (player == null) return;

                AddFollowingFriendToSafeArea(player, friendMob);
            }
            else if (baArgs.Following)
            {
            }
        }

        private void AddFollowingFriendToSafeArea(GamePlayer owner, FollowingFriendMob friendMob)
        {
            // Figure out if this is a solo or a group scenario for scoreboard
            var scoreRecord = GetScoreRecord(owner);
            float speed = friendMob.MaxSpeed <= 0 ? 1 : friendMob.MaxSpeed;
            float X = 200f / speed;
            float Y = friendMob.AggroMultiplier;
            double rawPoints = (Y <= 1.0f) ? 10.0 * X : 10.0 * X * Y;
            int basePoints = (int)Math.Round(rawPoints);

            scoreRecord.Friends_BroughtFriendsPoints += basePoints;

            // Family / guild bonus if friendMob.Guild is set
            string familyGuildName = friendMob.GuildName;
            if (!string.IsNullOrEmpty(familyGuildName))
            {
                bool isSolo = (owner.Group == null || owner.Group.MemberCount <= 1);
                AbstractArea area = null;

                if (isSolo)
                    _soloAreas.TryGetValue(owner, out area);
                else
                    _groupAreas.TryGetValue(owner.Group!, out area);

                if (area is PvpSafeArea safeArea)
                {
                    int oldCount = safeArea.GetFamilyCount(familyGuildName);
                    int newCount = oldCount + 1;
                    safeArea.SetFamilyCount(familyGuildName, newCount);

                    int totalFamInZone = CountFamilyInZone(friendMob);

                    if (newCount >= totalFamInZone)
                    {
                        int finalBonus = GetFamilyBonus(totalFamInZone);
                        scoreRecord.Friends_BroughtFamilyBonus += finalBonus;
                    }
                }
            }

            owner.Out.SendMessage(LanguageMgr.GetTranslation(owner.Client.Account.Language, "PvPManager.BroughtToSafety", friendMob.Name, basePoints), eChatType.CT_SpellExpires, eChatLoc.CL_SystemWindow);
        }

        /// <summary>
        /// Return the family bonus that applies if 'count' members of the same family are in the area.
        /// </summary>
        private int GetFamilyBonus(int count)
        {
            if (count < 2) return 0;
            if (count == 2) return 2;
            if (count == 3) return 6;
            if (count == 4) return 10;
            if (count == 5 || count == 6) return 15;
            return 20;
        }

        /// <summary>
        /// Count how many FollowingFriendMobs with the same GuildName
        /// are in the *same zone* as 'friendMob'.
        /// </summary>
        private int CountFamilyInZone(FollowingFriendMob friendMob)
        {
            if (friendMob == null || string.IsNullOrEmpty(friendMob.GuildName))
                return 0;

            var zone = friendMob.CurrentZone;
            if (zone == null) return 0;

            // Gather all NPCs in the region, filter by same zone + same guild
            var regionID = zone.ZoneRegion.ID;
            var allNpcs = WorldMgr.GetNPCsFromRegion(regionID).Where(n => n.CurrentZone == zone && n is FollowingFriendMob ff && ff.GuildName == friendMob.GuildName);
            return allNpcs.Count();
        }

        private void OnLivingDying_BringAFriend(DOLEvent e, object sender, EventArgs args)
        {
            if (!_isOpen || _activeSession == null || _activeSession.SessionType != 4)
                return;

            if (!(sender is FollowingFriendMob friendMob)) return;

            GameLiving killer = null;
            if (args is DyingEventArgs dyingArgs && dyingArgs.Killer is GameLiving gl)
            {
                killer = gl;
            }

            // 1) If friendMob was actively following a player => that player/team loses 2 points
            var followedPlayer = friendMob.PlayerFollow;
            if (followedPlayer != null && killer is GamePlayer killerPlayer && IsInActivePvpZone(followedPlayer))
            {
                if (killerPlayer != followedPlayer)
                {
                    var followedScore = GetScoreRecord(followedPlayer);
                    followedScore.Friends_FriendKilledCount++;
                    followedScore.Friends_FriendKilledPoints += 2; // penalty
                }
            }

            // 2) If the *killer* is a player, and the mob was following someone => killer gets +2
            if (killer is GamePlayer kp && friendMob.PlayerFollow != null && IsInActivePvpZone(kp))
            {
                if (kp != friendMob.PlayerFollow)
                {
                    var killerScore = GetScoreRecord(kp);
                    killerScore.Friends_KillEnemyFriendCount++;
                    killerScore.Friends_KillEnemyFriendPoints += 2;
                }
            }
        }
        #endregion

        #region CaptureTerritories Methods
        private int AwardTerritoryOwnershipPoints(RegionTimer timer)
        {
            // 1) Make sure session is open, type=5
            if (!_isOpen || _activeSession == null || _activeSession.SessionType != 5)
                return 30000;

            // 2) Figure out which zone IDs are in the session
            var zoneIDs = CurrentZones.Select(z => z.ID);

            var subterritories = TerritoryManager.Instance.Territories
                .Where(t => t.Type == Territory.eType.Subterritory && t.OwnerGuild != null).ToList();

            foreach (var territory in subterritories)
            {
                if (!IsTerritoryInSessionZones(territory, zoneIDs))
                    continue;

                var owningGuild = territory.OwnerGuild;
                if (owningGuild == null)
                    continue;

                foreach (var member in owningGuild.GetListOfOnlineMembers())
                {
                    if (member.IsInPvP && member.Client != null && member.CurrentRegion != null && PvpManager.Instance.IsInActivePvpZone(member))
                    {
                        var score = GetScoreRecord(member);
                        score.Terr_TerritoriesOwnershipPoints += 1;
                    }
                }
            }

            return 30000;
        }

        private bool IsTerritoryInSessionZones(Territory territory, IEnumerable<ushort> zoneIDs)
        {
            if (territory.Zone == null)
                return false;
            return zoneIDs.Contains(territory.Zone.ID);
        }
        #endregion

        #region Old Compatibility Methods
        /// <summary>
        /// For old code referencing IsPvPRegion(ushort),
        /// we define "PvP region" as the zones in the current session's ZoneList.
        /// </summary>
        public bool IsPvPRegion(ushort regionID)
        {
            if (_activeSession == null)
                return false;

            return CurrentZones.Select(z => z.ID).Contains(regionID);
        }

        /// <summary>
        /// If the living is a GamePlayer, returns IsInPvP flag
        /// </summary>
        public bool IsIn(GameLiving living)
        {
            if (living is GamePlayer plr)
                return plr.IsInPvP;
            return false;
        }
        #endregion

        #region PlayerScores
        public class PlayerScore
        {
            // --- Universal (for all sessions) ---
            // We'll keep track of the player's name/ID just for clarity
            public string PlayerName { get; set; }
            public string PlayerID { get; set; }

            // --- For session type #1: PvP Combat ---
            public int PvP_SoloKills { get; set; }
            public int PvP_SoloKillsPoints { get; set; }
            public int PvP_GroupKills { get; set; }
            public int PvP_GroupKillsPoints { get; set; }

            // --- For session type #2: Flag Capture ---
            public int Flag_SoloKills { get; set; }
            public int Flag_SoloKillsPoints { get; set; }
            public int Flag_GroupKills { get; set; }
            public int Flag_GroupKillsPoints { get; set; }
            public int Flag_KillFlagCarrierCount { get; set; }
            public int Flag_KillFlagCarrierPoints { get; set; }
            public int Flag_FlagReturnsCount { get; set; }
            public int Flag_FlagReturnsPoints { get; set; }
            public int Flag_OwnershipPoints { get; set; }

            // --- For session type #3: Treasure Hunt ---
            public int Treasure_SoloKills { get; set; }
            public int Treasure_SoloKillsPoints { get; set; }
            public int Treasure_GroupKills { get; set; }
            public int Treasure_GroupKillsPoints { get; set; }
            public int Treasure_BroughtTreasuresPoints { get; set; }
            public int Treasure_StolenTreasuresPoints { get; set; }

            // --- For session type #4: Bring Friends ---
            public int Friends_SoloKills { get; set; }
            public int Friends_SoloKillsPoints { get; set; }
            public int Friends_GroupKills { get; set; }
            public int Friends_GroupKillsPoints { get; set; }
            public int Friends_BroughtFriendsPoints { get; set; }
            public int Friends_BroughtFamilyBonus { get; set; }
            public int Friends_FriendKilledCount { get; set; }
            public int Friends_FriendKilledPoints { get; set; }
            public int Friends_KillEnemyFriendCount { get; set; }
            public int Friends_KillEnemyFriendPoints { get; set; }

            // --- For session type #5: Capture Territories ---
            public int Terr_SoloKills { get; set; }
            public int Terr_SoloKillsPoints { get; set; }
            public int Terr_GroupKills { get; set; }
            public int Terr_GroupKillsPoints { get; set; }
            public int Terr_TerritoriesCapturedCount { get; set; }
            public int Terr_TerritoriesCapturedPoints { get; set; }
            public int Terr_TerritoriesOwnershipPoints { get; set; }

            // --- For session type #6: Boss Kill Cooperation ---
            public int Boss_SoloKills { get; set; }
            public int Boss_SoloKillsPoints { get; set; }
            public int Boss_GroupKills { get; set; }
            public int Boss_GroupKillsPoints { get; set; }
            public int Boss_BossHitsCount { get; set; }
            public int Boss_BossHitsPoints { get; set; }
            public int Boss_BossKillsCount { get; set; }
            public int Boss_BossKillsPoints { get; set; }

            /// <summary>
            /// Helper: returns total points for the current session type,
            /// summing only the relevant fields.
            /// You can expand this logic to handle any special bonus, etc.
            /// </summary>
            public int GetTotalPoints(int sessionType)
            {
                switch (sessionType)
                {
                    case 1: // PvP Combats
                        return PvP_SoloKillsPoints + PvP_GroupKillsPoints;

                    case 2: // Flag Capture
                        return Flag_SoloKillsPoints +
                               Flag_GroupKillsPoints +
                               Flag_KillFlagCarrierPoints +
                               Flag_FlagReturnsPoints +
                               Flag_OwnershipPoints;

                    case 3: // Treasure Hunt
                        return Treasure_SoloKillsPoints +
                               Treasure_GroupKillsPoints +
                               Treasure_BroughtTreasuresPoints -
                               Treasure_StolenTreasuresPoints;

                    case 4: // Bring Friends
                        return Friends_SoloKillsPoints +
                               Friends_GroupKillsPoints +
                               Friends_BroughtFriendsPoints +
                               Friends_BroughtFamilyBonus -
                               Friends_FriendKilledPoints +
                               Friends_KillEnemyFriendPoints;

                    case 5: // Capture Territories
                        return Terr_SoloKillsPoints +
                               Terr_GroupKillsPoints +
                               Terr_TerritoriesCapturedPoints +
                               Terr_TerritoriesOwnershipPoints;

                    case 6: // Boss Kill Cooperation
                        return Boss_SoloKillsPoints +
                               Boss_GroupKillsPoints +
                               Boss_BossHitsPoints +
                               Boss_BossKillsPoints;

                    default:
                        return 0;
                }
            }
        }

        public record GroupScoreEntry(PlayerScore PlayerScore, DateTime TimeJoined);

        public record GroupScore(Guild Guild, PlayerScore Totals, Dictionary<string, GroupScoreEntry> Scores)
        {
            public int GetTotalPoints(int sessionType)
            {
                return Scores.Values.Select(e => e.PlayerScore.GetTotalPoints(sessionType)).Aggregate(0, (a, b) => a + b);
            }

            public int PlayerCount => Scores.Count;
        }
        #endregion

        enum ScoreType
        {
            Bonus,
            /// <summary>
            /// Don't display count, display negative points
            /// </summary>
            Malus,
            /// <summary>
            /// Don't display count
            /// </summary>
            BonusPoints,
            /// <summary>
            /// Don't display count, display negative points
            /// </summary>
            MalusPoints
        }

        private record Score(int Points, int Count, ScoreType Type = ScoreType.Bonus);

        private record ScoreLine(string Label, Score Points)
        {
            
            /// <inheritdoc />
            public override string ToString()
            {
                return base.ToString();
            }

            public string ToString(string language, bool shortDescription)
            {
                if (shortDescription)
                {
                    var translated = LanguageMgr.GetTranslation(language, Label + ".Short");
                    return Points.Type switch
                    {
                        ScoreType.Bonus => $"{translated}={Points.Count}({LanguageMgr.GetTranslation(language, "PvPManager.Score.Pts", Points.Points)})",
                        ScoreType.Malus => $"{translated}={Points.Count}(-{LanguageMgr.GetTranslation(language, "PvPManager.Score.Pts", Points.Points)})",
                        ScoreType.BonusPoints => $"{translated}={LanguageMgr.GetTranslation(language, "PvPManager.Score.Pts", Points.Points)}",
                        ScoreType.MalusPoints => $"{translated}=-{LanguageMgr.GetTranslation(language, "PvPManager.Score.Pts", Points.Points)}",
                    };
                }
                else
                {
                    var translated = LanguageMgr.GetTranslation(language, Label);
                    return Points.Type switch
                    {
                        ScoreType.Bonus => $"    {translated}: {Points.Count} - {LanguageMgr.GetTranslation(language, "PvPManager.Score.Points", Points.Points)}",
                        ScoreType.Malus => $"    {translated}: {Points.Count} - -{LanguageMgr.GetTranslation(language, "PvPManager.Score.Points", Points.Points)}",
                        ScoreType.BonusPoints => $"    {translated}: {LanguageMgr.GetTranslation(language, "PvPManager.Score.Points", Points.Points)}",
                        ScoreType.MalusPoints => $"    {translated}: -{LanguageMgr.GetTranslation(language, "PvPManager.Score.Points", Points.Points)}",
                    };
                }
            }
        }

        private record ScoreboardEntry(string Player, int Total, List<ScoreLine> Lines);

        private ScoreboardEntry MakeScoreboardEntry(PlayerScore ps)
        {
            if (!IsOpen)
                return null;
            
            var sessionType = _activeSession!.SessionType;
            List<ScoreLine> scoreLines = new();
            switch (sessionType)
            {
                case 1: // Pure PvP Combat
                    scoreLines.Add(new ScoreLine("PvP.Score.PvPSoloKills", new Score(ps.PvP_SoloKillsPoints, ps.PvP_SoloKills)));
                    scoreLines.Add(new ScoreLine("PvP.Score.PvPGrpKills", new Score(ps.PvP_GroupKillsPoints, ps.PvP_GroupKills)));
                    break;

                case 2: // Flag Capture
                    scoreLines.Add(new ScoreLine("PvP.Score.FlagPvPSoloKills", new Score(ps.Flag_SoloKillsPoints, ps.Flag_SoloKills)));
                    scoreLines.Add(new ScoreLine("PvP.Score.FlagPvPGrpKills", new Score(ps.Flag_GroupKillsPoints, ps.Flag_GroupKills)));
                    scoreLines.Add(new ScoreLine("PvP.Score.FlagPvPFlagCarrierKillBonus", new Score(ps.Flag_KillFlagCarrierPoints, ps.Flag_KillFlagCarrierCount)));
                    scoreLines.Add(new ScoreLine("PvP.Score.FlagPvPFlagsCaptured", new Score(ps.Flag_FlagReturnsPoints, ps.Flag_FlagReturnsCount)));
                    scoreLines.Add(new ScoreLine("PvP.Score.FlagPvPOwnership", new Score(ps.Flag_OwnershipPoints, 0, ScoreType.BonusPoints)));
                    break;

                case 3: // Treasure Hunt
                    scoreLines.Add(new ScoreLine("PvP.Score.TreasurePvPSoloKills", new Score(ps.Treasure_SoloKillsPoints, ps.Treasure_SoloKills)));
                    scoreLines.Add(new ScoreLine("PvP.Score.TreasurePvPGrpKills", new Score(ps.Treasure_GroupKillsPoints, ps.Treasure_GroupKills)));
                    scoreLines.Add(new ScoreLine("PvP.Score.TreasurePvPTreasurePoints", new Score(ps.Treasure_BroughtTreasuresPoints, 0, ScoreType.BonusPoints)));
                    scoreLines.Add(new ScoreLine("PvP.Score.TreasurePvPStolenItemPenalty", new Score(ps.Treasure_StolenTreasuresPoints, 0, ScoreType.MalusPoints)));
                    break;

                case 4: // Bring Friends
                    scoreLines.Add(new ScoreLine("PvP.Score.FriendsPvPSoloKills", new Score(ps.Friends_SoloKillsPoints, ps.Friends_SoloKills)));
                    scoreLines.Add(new ScoreLine("PvP.Score.FriendsPvPGrpKills", new Score(ps.Friends_GroupKillsPoints, ps.Friends_GroupKills)));
                    scoreLines.Add(new ScoreLine("PvP.Score.FriendsPvPBroughtFriends", new Score(ps.Friends_BroughtFriendsPoints, 0, ScoreType.BonusPoints)));
                    scoreLines.Add(new ScoreLine("PvP.Score.FriendsPvPFamilyBonus", new Score(ps.Friends_BroughtFamilyBonus, 0, ScoreType.BonusPoints)));
                    scoreLines.Add(new ScoreLine("PvP.Score.FriendsPvPLostFriends", new Score(ps.Friends_FriendKilledPoints, ps.Friends_FriendKilledCount)));
                    scoreLines.Add(new ScoreLine("PvP.Score.FriendsPvPKilledOthersFriends", new Score(ps.Friends_KillEnemyFriendPoints, ps.Friends_KillEnemyFriendCount)));
                    break;

                case 5: // Capture Territories
                    scoreLines.Add(new ScoreLine("PvP.Score.CTTPvPSoloKills", new Score(ps.Terr_SoloKillsPoints, ps.Terr_SoloKills)));
                    scoreLines.Add(new ScoreLine("PvP.Score.CTTPvPGrpKills", new Score(ps.Terr_GroupKillsPoints, ps.Terr_GroupKills)));
                    scoreLines.Add(new ScoreLine("PvP.Score.CTTPvPTerritoryCaptures", new Score(ps.Terr_TerritoriesCapturedPoints, ps.Terr_TerritoriesCapturedCount)));
                    scoreLines.Add(new ScoreLine("PvP.Score.CTTPvPOwnership", new Score(ps.Terr_TerritoriesOwnershipPoints, 0)));
                    break;

                case 6: // Boss Kill
                    scoreLines.Add(new ScoreLine("PvP.Score.BossPvPSoloBossKills", new Score(ps.Boss_SoloKillsPoints, ps.Boss_SoloKills)));
                    scoreLines.Add(new ScoreLine("PvP.Score.BossPvPGroupBossKills", new Score(ps.Boss_GroupKillsPoints, ps.Boss_GroupKills)));
                    scoreLines.Add(new ScoreLine("PvP.Score.BossPvPBossHits", new Score(ps.Boss_BossHitsPoints, ps.Boss_BossHitsCount)));
                    scoreLines.Add(new ScoreLine("PvP.Score.BossPvPBossKills", new Score(ps.Boss_BossKillsPoints, ps.Boss_BossKillsCount)));
                    break;

                default:
                    break;
            }
            scoreLines.Add(new ScoreLine("PvP.Score.Total", new Score(ps.GetTotalPoints(sessionType), 0, ScoreType.BonusPoints)));
            return new ScoreboardEntry(ps.PlayerName, ps.GetTotalPoints(sessionType), scoreLines);
        }

        #region Stats
        
        [return: NotNull]
        private PlayerScore GetPlayerScore(GamePlayer viewer)
        {
            if (_playerScores.TryGetValue(viewer.InternalID, out var myScore))
            {
                return myScore!;
            }
            else
            {
                return new PlayerScore() { PlayerID = viewer.InternalID, PlayerName = viewer.Name };
            }
        }

        [return: NotNull]
        private IEnumerable<PlayerScore> GetGroupScore(GamePlayer viewer)
        {
            IEnumerable<PlayerScore> list = Enumerable.Empty<PlayerScore>();
            if (_activeSession != null && viewer.Guild != null)
            {
                if (_groupScores.TryGetValue(viewer.Guild, out GroupScore groupScore))
                {
                    list = viewer.Guild.GetListOfOnlineMembers()
                        .Select(p => groupScore.Scores.GetValueOrDefault(p.InternalID)?.PlayerScore ?? new PlayerScore()
                        {
                            PlayerID = p.InternalID,
                            PlayerName = p.Name
                        })
                        .OrderBy(s => s.GetTotalPoints(_activeSession.SessionType))
                        .Prepend(groupScore.Totals);
                }
                else
                {
                    list = viewer.Guild.GetListOfOnlineMembers()
                        .Select(p => new PlayerScore()
                        {
                            PlayerID = p.InternalID,
                            PlayerName = p.Name
                        })
                        .Prepend(groupScore.Totals);
                }
            }
            return list;
        }

        private void AddLines(List<string> lines, ScoreboardEntry entry, string language, bool shortStats)
        {
            if (shortStats)
            {
                lines.Add($"  {entry.Player}: " + string.Join(", ", entry.Lines.Select(l => l.ToString(language, shortStats))));
            }
            else
            {
                lines.Add($"  {entry.Player}:");
                lines.AddRange(
                    entry.Lines.Select(
                        l => l.ToString(language, shortStats)
                    )
                );
            }
        }
        
        public IList<string> GetStatistics(GamePlayer viewer, bool all = false)
        {
            var lines = new List<string>();
            string sessionTypeString = "Unknown";
            if (CurrentSession != null)
            {
                switch (CurrentSession.SessionType)
                {
                    case 1:
                        sessionTypeString = "PvP Combats";
                        break;
                    case 2:
                        sessionTypeString = "Flag Capture";
                        break;
                    case 3:
                        sessionTypeString = "Treasure Hunt";
                        break;
                    case 4:
                        sessionTypeString = "Bring Friends";
                        break;
                    case 5:
                        sessionTypeString = "Capture Territories";
                        break;
                    case 6:
                        sessionTypeString = "Boss Kill Cooperation";
                        break;
                    default:
                        sessionTypeString = "Unknown";
                        break;
                }
            }

            if (!IsOpen)
            {
                if (viewer.Client.Account.PrivLevel == 1)
                {
                    lines.Add("PvP is CLOSED.");
                }
            }
            else
            {
                if (CurrentZones.Any())
                {
                    foreach (Zone z in CurrentZones)
                    {
                        if (z != null)
                        {
                            string zoneName = !string.IsNullOrEmpty(z.Description) ? z.Description : $"Zone#{z.ID}";
                            lines.Add(sessionTypeString + " in " + zoneName);
                        }
                        else
                        {
                            lines.Add(sessionTypeString + " in Unknown Zone");
                        }
                    }
                }
            }
            lines.Add("");

            if (viewer.Client.Account.PrivLevel > 1)
            {
                lines.Add("");
                lines.Add("-------------------------------------------------------------");
                lines.Add("PvP is " + (IsOpen ? "OPEN" : "CLOSED") + ".");
                lines.Add("Session ID: " + (CurrentSession?.SessionID ?? "(none)"));
                lines.Add("Forced Open: " + _isForcedOpen);
                lines.Add("Session Type: " + sessionTypeString);
                lines.Add("");

                if (_isOpen)
                {
                    if (CurrentZones.Any())
                    {
                        lines.Add("Zones in this PvP session:");
                        foreach (Zone z in CurrentZones)
                        {
                            if (z != null)
                            {
                                string zoneName = !string.IsNullOrEmpty(z.Description)
                                    ? z.Description
                                    : $"Zone#{z.ID}";
                                lines.Add("  > " + zoneName);
                            }
                            else
                            {
                                lines.Add($"  (Unknown Zone)");
                            }
                        }
                    }
                    else
                    {
                        lines.Add("No zones currently configured in the session.");
                    }
                }
                lines.Add("-------------------------------------------------------------");
                lines.Add("");
                lines.Add("");
            }

            // Show scoreboard
            if (!IsOpen)
            {
                lines.Add("No scoreboard data yet!");
            }
            else
            {
                IEnumerable<PlayerScore> scores = Enumerable.Empty<PlayerScore>();
                PlayerScore groupTotal = null;
                // We want to sort players by total points descending
                var sessionType = CurrentSession!.SessionType;
                List<ScoreboardEntry> scoreLines;
                var language = viewer.Client.Account.Language;
                bool shortStats = all;
                if (all)
                {
                    if (CurrentSession.GroupCompoOption == 1 || CurrentSession.GroupCompoOption == 3)
                    {
                        // TODO: Don't take solo players if they are part of a group?
                        scores = _playerScores.Values;
                    }
                    if (CurrentSession.GroupCompoOption == 2 || CurrentSession.GroupCompoOption == 3)
                    {
                        scores = scores.Concat(_groupScores.Values.Select(s => s.Totals));
                    }
                    
                    lines.Add("Current Scoreboard:");
                    scoreLines = scores
                        .OrderByDescending(s => s.GetTotalPoints(sessionType))
                        .Select(MakeScoreboardEntry)
                        .ToList();

                    foreach (var ps in scoreLines)
                    {
                        AddLines(lines, ps, language, shortStats);
                    }
                }
                else
                {
                    var myScores = GetPlayerScore(viewer);
                    var ourScores = Enumerable.Empty<PlayerScore>();
                    if (CurrentSession.GroupCompoOption == 2 || CurrentSession.GroupCompoOption == 3)
                    {
                        ourScores = GetGroupScore(viewer);
                    }

                    if (ourScores.Any())
                    {
                        lines.Add($"Current Scoreboard for {viewer.Guild?.Name ?? viewer.Name}:");
                        foreach (var ps in ourScores.Select(MakeScoreboardEntry))
                        {
                            AddLines(lines, ps, language, shortStats);
                        }
                    }
                    
                    lines.Add("");
                    lines.Add("Your individual score: ");
                    AddLines(lines, MakeScoreboardEntry(myScores), language, shortStats);
                }

                lines.Add("");
                lines.Add("");
                lines.Add("");
                lines.Add($"Waiting queue: {_soloQueue.Count} players");
                lines.Add($"Session Max Group size: {CurrentSession?.GroupMaxSize} players");
                lines.Add("");

                int inPvP = 0;
                foreach (var c in WorldMgr.GetAllPlayingClients())
                {
                    if (c?.Player != null && c.Player.IsInPvP)
                        inPvP++;
                }
                lines.Add($"Players currently in PvP: {inPvP}");
            }

            return lines;
        }
        #endregion
    }
}