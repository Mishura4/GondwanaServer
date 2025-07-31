using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Amte;
using AmteScripts.Utils;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Geometry;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;
using DOL.Territories;
using DOL.GS.Keeps;
using log4net;

namespace AmteScripts.Managers
{
    public class RvrManager
    {
        const string ALBION = "Albion";
        const string HIBERNIA = "Hibernia";
        const string MIDGARD = "Midgard";

        //const string RvRNoviceALB = "RvR-Novice-ALB";
        //const string RvRNoviceHIB = "RvR-Novice-HIB";
        //const string RvRNoviceMID = "RvR-Novice-MID";

        const string RvRDebutantALB = "RvR-Debutant-ALB";
        const string RvRDebutantHIB = "RvR-Debutant-HIB";
        const string RvRDebutantMID = "RvR-Debutant-MID";

        const string RvRStandardALB = "RvR-Standard-ALB";
        const string RvRStandardHIB = "RvR-Standard-HIB";
        const string RvRStandardMID = "RvR-Standard-MID";

        const string RvRExpertALB = "RvR-Expert-ALB";
        const string RvRExpertHIB = "RvR-Expert-HIB";
        const string RvRExpertMID = "RvR-Expert-MID";

        private static readonly string[] MasterMapPrefixes = new string[] { "Master01", "Master02", "Master03" };
        private static readonly string[] RvRMasterSpawns = new string[]
        {
            "RvR-Master01-ALB", "RvR-Master01-HIB", "RvR-Master01-MID",
            "RvR-Master02-ALB", "RvR-Master02-HIB", "RvR-Master02-MID",
            "RvR-Master03-ALB", "RvR-Master03-HIB", "RvR-Master03-MID"
        };

        //const string RvRDivineALB = "RvR-Divine-ALB";
        //const string RvRDivineHIB = "RvR-Divine-HIB";
        //const string RvRDivineMID = "RvR-Divine-MID";

        private static int RVR_RADIUS = Properties.RvR_AREA_RADIUS;
        private static DateTime _startTime = DateTime.Today.AddHours(20D); //20H00
        private static DateTime _endTime = _startTime.Add(TimeSpan.FromHours(4)).Add(TimeSpan.FromMinutes(5)); //4H00 + 5
        private const int _checkInterval = 30 * 1000; // 30 seconds
        private static readonly Position _stuckSpawn = Position.Create(51, 434303, 493165, 3088, 1069);
        private Dictionary<ushort, IList<string>> RvrStats = new Dictionary<ushort, IList<string>>();
        private Dictionary<string, int> Scores = new Dictionary<string, int>();
        private Dictionary<GamePlayer, short> kills = new Dictionary<GamePlayer, short>();
        private Dictionary<string, Dictionary<eRealm, TimeSpan>> holdingTimesByZone = new Dictionary<string, Dictionary<eRealm, TimeSpan>>();
        private int checkScore = 0;
        private int checkNumberOfPlayer = 0;
        private string winnerName = "";
        private DateTime RvRBonusDate = DateTime.Now.Date;

        private string _currentMasterMap = null;
        public string CurrentMasterMap => _currentMasterMap;

        #region Static part
        private static readonly Random _rng = new Random();
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private static RvrManager _instance;
        private static RegionTimer _timer;

        public static RvrManager Instance { get { return _instance; } }

        [ScriptLoadedEvent]
        public static void OnServerStarted(DOLEvent e, object sender, EventArgs args)
        {
            log.Info("RvRManger: Started");
            _instance = new RvrManager();
            _instance.Scores.Add(ALBION, 0);
            _instance.Scores.Add(HIBERNIA, 0);
            _instance.Scores.Add(MIDGARD, 0);
            _timer = new RegionTimer(WorldMgr.GetRegion(1).TimeManager)
            {
                Callback = _instance._CheckRvr
            };
            _timer.Start(10000);
        }

        [ScriptUnloadedEvent]
        public static void OnServerStopped(DOLEvent e, object sender, EventArgs args)
        {
            log.Info("RvRManger: Stopped");
            _timer.Stop();
        }
        #endregion

        private bool _isOpen;
        private bool _isForcedOpen;
        private IEnumerable<ushort> _regions = Enumerable.Empty<ushort>();

        private readonly Guild _albion;
        private readonly Guild _midgard;
        private readonly Guild _hibernia;

        public bool IsOpen { get { return _isOpen; } }
        public IEnumerable<ushort> Regions { get { return _regions; } }

        public static eRealm WinnerRealm
        {
            get
            {
                switch (Instance.winnerName)
                {
                    case ALBION:
                        return eRealm.Albion;
                    case MIDGARD:
                        return eRealm.Midgard;
                    case HIBERNIA:
                        return eRealm.Hibernia;
                    default:
                        return eRealm.None;
                }
            }
        }

        public Dictionary<GamePlayer, short> Kills
        {
            get
            {
                return kills;
            }
            set
            {
                kills = value;
            }
        }

        /// <summary>
        /// Key: spawn/map name (e.g., "RvR-Debutant-ALB", "RvR-Master01-MID")
        /// </summary>
        private readonly Dictionary<string, RvRMap> _maps = new Dictionary<string, RvRMap>();

        private RvrManager()
        {
            _albion = GuildMgr.GetGuildByName(ALBION);
            if (_albion == null)
                _albion = GuildMgr.CreateGuild(eRealm.Albion, ALBION);

            _hibernia = GuildMgr.GetGuildByName(HIBERNIA);
            if (_hibernia == null)
                _hibernia = GuildMgr.CreateGuild(eRealm.Hibernia, HIBERNIA);

            _midgard = GuildMgr.GetGuildByName(MIDGARD);
            if (_midgard == null)
                _midgard = GuildMgr.CreateGuild(eRealm.Midgard, MIDGARD);

            _albion.SaveIntoDatabase();
            _midgard.SaveIntoDatabase();
            _hibernia.SaveIntoDatabase();
            InitMapsAndTerritories();

            foreach (var mapKey in _maps.Keys)
            {
                if (!holdingTimesByZone.ContainsKey(mapKey))
                {
                    holdingTimesByZone[mapKey] = new Dictionary<eRealm, TimeSpan>
                    {
                        { eRealm.Albion, TimeSpan.Zero },
                        { eRealm.Midgard, TimeSpan.Zero },
                        { eRealm.Hibernia, TimeSpan.Zero }
                    };
                }
            }
        }

        public void OnControlChange(string lordId, Guild guild)
        {
            var map = _maps.Values.FirstOrDefault(m => m.RvRTerritory != null && m.RvRTerritory.BossId.Equals(lordId));

            if (map != null)
            {
                map.RvRTerritory.OwnerGuild = guild;
                map.RvRTerritory.ToggleBanner(true);
                map.RvRTerritory.Mobs.ForEach(m =>
                {
                    m.GuildName = guild.Name;
                    m.Realm = guild.Realm;
                });
                map.RvRTerritory.Boss.Realm = guild.Realm;
                AbstractGameKeep keep = GameServer.KeepManager.GetKeepCloseToSpot(map.RvRTerritory.Boss.Position, 100000);
                keep.TempRealm = guild.Realm;
                keep.Reset(guild.Realm);
                keep.Guild = guild;
                // reset all doors
                foreach (GameKeepDoor door in keep.Doors.Values)
                {
                    door.Reset(guild.Realm);
                }
            }
        }

        public RvRTerritory GetRvRTerritory(ushort regionId)
        {
            var map = this._maps.Values.FirstOrDefault(v => v.RvRTerritory != null && v.Position.RegionID.Equals(regionId));

            return map?.RvRTerritory;
        }

        public IEnumerable<ushort> InitMapsAndTerritories()
        {
            var npcs = WorldMgr.GetNPCsByGuild("RVR", eRealm.None).Where(n => n.Name.StartsWith("RvR-"));
            _maps.Clear();

            //var RvRNovices = npcs.Where(n => n.Name.StartsWith("RvR-Novice"));
            var RvRDebutants = npcs.Where(n => n.Name.StartsWith("RvR-Novice"));
            var RvRStandards = npcs.Where(n => n.Name.StartsWith("RvR-Debutant"));
            var RvRExperts = npcs.Where(n => n.Name.StartsWith("RvR-Expert"));
            var RvRMasters = npcs.Where(n => RvRMasterSpawns.Contains(n.Name));
            //var RvRDivines = npcs.Where(n => n.Name.StartsWith("RvR-Divine"));

            if (RvRDebutants == null || RvRStandards == null || RvRExperts == null || RvRMasters == null)
            {
                throw new KeyNotFoundException("RvR Maps");
            }

            //RvRNovices.Foreach(novice =>
            //{
            //    string name = null;
            //    var map = this.BuildRvRMap(novice);

            //    if (map == null) {  /*Skip Null Map*/ return; }

            //    if (novice.Name.EndsWith("HIB"))
            //    {
            //        name = RvRNoviceHIB;
            //    }
            //    else if (novice.Name.EndsWith("ALB"))
            //    {
            //        name = RvRNoviceALB;
            //    }
            //    else if (novice.Name.EndsWith("MID"))
            //    {
            //        name = RvRNoviceMID;
            //    }
            //    _maps.Add(name, map);
            //});

            RvRDebutants.Foreach(debutant =>
            {
                string name = null;
                var map = this.BuildRvRMap(debutant);

                if (map == null) {  /*Skip Null Map*/ return; }

                if (debutant.Name.EndsWith("HIB"))
                {
                    name = RvRDebutantHIB;
                }
                else if (debutant.Name.EndsWith("ALB"))
                {
                    name = RvRDebutantALB;
                }
                else if (debutant.Name.EndsWith("MID"))
                {
                    name = RvRDebutantMID;
                }
                _maps.Add(name!, map);
            });


            RvRStandards.Foreach(standard =>
            {
                string name = null;
                var map = this.BuildRvRMap(standard);

                if (map == null) {  /*Skip Null Map*/ return; }

                if (standard.Name.EndsWith("HIB"))
                {
                    name = RvRStandardHIB;
                }
                else if (standard.Name.EndsWith("ALB"))
                {
                    name = RvRStandardALB;
                }
                else if (standard.Name.EndsWith("MID"))
                {
                    name = RvRStandardMID;
                }
                _maps.Add(name!, map);
            });


            RvRExperts.Foreach(expert =>
            {
                string name = null;
                var map = this.BuildRvRMap(expert);

                if (map == null) {  /*Skip Null Map*/ return; }

                if (expert.Name.EndsWith("HIB"))
                {
                    name = RvRExpertHIB;
                }
                else if (expert.Name.EndsWith("ALB"))
                {
                    name = RvRExpertALB;
                }
                else if (expert.Name.EndsWith("MID"))
                {
                    name = RvRExpertMID;
                }
                _maps.Add(name!, map);
            });


            RvRMasters.Foreach(master =>
            {
                var map = BuildRvRMap(master);
                if (map == null) return;
                _maps[master.Name] = map;
            });

            //RvRDivines.Foreach(divine =>
            //{
            //    string name = null;
            //    var map = this.BuildRvRMap(divine);

            //    if (map == null) {  /*Skip Null Map*/ return; }

            //    if (divine.Name.EndsWith("HIB"))
            //    {
            //        name = RvRDivineHIB;
            //    }
            //    else if (divine.Name.EndsWith("ALB"))
            //    {
            //        name = RvRDivineALB;
            //    }
            //    else if (divine.Name.EndsWith("MID"))
            //    {
            //        name = RvRDivineMID;
            //    }
            //    _maps.Add(name, map);
            //});

            _regions = _maps.Values.GroupBy(v => v.Position.RegionID).Select(v => v.Key).OrderBy(v => v);
            _regions.Foreach(r => this.RvrStats[r] = new string[] { });

            return from m in _maps select m.Value.Position.RegionID;
        }

        private RvRMap BuildRvRMap(GameNPC initNpc)
        {
            RvRTerritory rvrTerritory = null;
            if (!_maps.Values.Any(v => v.Position.RegionID.Equals(initNpc.CurrentRegionID)))
            {
                var lord = (LordRvR)(initNpc.CurrentRegion.Objects.FirstOrDefault(o => o is LordRvR));

                if (lord == null)
                {
                    log.Error("Cannot Init RvR because no LordRvR was present in Region " + initNpc.CurrentRegionID + " for InitNpc: " + initNpc.Name + ". Add a LordRvR in this RvR");
                    return null;
                }

                var areaName = string.IsNullOrEmpty(lord.GuildName) ? initNpc.Name : lord.GuildName;
                //var areaName = "";
                var area = new Area.Circle(areaName, lord.Position.X, lord.Position.Y, lord.Position.Z, RVR_RADIUS);
                rvrTerritory = new RvRTerritory(lord.CurrentZone, new List<IArea> { area }, area.Description, lord, area.Coordinate, lord.CurrentRegionID, null);
            }

            return new RvRMap()
            {
                Position = initNpc.Position,
                RvRTerritory = rvrTerritory
            };
        }

        public void OnPlayerLogIn(GamePlayer player)
        {
            var rvr = GameServer.Database.SelectObject<RvrPlayer>(r => r.PlayerID == player.InternalID);
            if (rvr == null)
            {
                // Lost RvR data, oops, move the player out. Unfortunately this means we lost the player's real guild as well
                if (player.Client.Account.PrivLevel > 1)
                {
                    // Leave GMs alone
                    return;
                }
                RemovePlayer(player, rvr);
            }

            if (!string.IsNullOrEmpty(rvr!.PvPSession))
                return;

            if (!string.IsNullOrEmpty(rvr.GuildID))
            {
                Guild guild = GuildMgr.GetGuildByGuildID(rvr.GuildID);

                if (guild != null)
                {
                    player.RealGuild = guild;
                }
                else if (GameServer.Instance.Logger.IsDebugEnabled)
                {
                    GameServer.Instance.Logger.DebugFormat("Could not find guild {0} for RvR player {1} ({2}) logging in", rvr.GuildID, player.Name, player.InternalID);
                }
            }
            if (!_isOpen && player.Client.Account.PrivLevel <= 1)
            {
                RemovePlayer(player, rvr);
            }
        }

        private DateTime lastHoldingUpdate = DateTime.Now;

        private int _CheckRvr(RegionTimer callingtimer)
        {
            Console.WriteLine("Check RVR");
            DateTime currentTime = DateTime.Now;
            var elapsed = currentTime - lastHoldingUpdate;
            lastHoldingUpdate = currentTime;

            if (_isOpen)
            {
                foreach (var kv in ActiveMaps())
                {
                    var zoneKey = kv.Key;
                    var map = kv.Value;
                    if (map.RvRTerritory != null && map.RvRTerritory.OwnerGuild != null)
                    {
                        holdingTimesByZone[zoneKey][map.RvRTerritory.OwnerGuild.Realm] += elapsed;
                    }
                }
            }

            if (!_isOpen)
            {
                _regions.Foreach(id => WorldMgr.GetClientsOfRegion(id).Foreach(p => RemovePlayer(p, false)));
                if (currentTime >= _startTime && currentTime < _endTime)
                    Open(false);
            }
            else
            {
                // Count the number of player in RvR
                int countPlayer = 0;

                foreach (var id in _regions)
                {
                    foreach (var cl in WorldMgr.GetClientsOfRegion(id))
                    {
                        if (cl.Player.Guild == null)
                        {
                            if (cl.Account.PrivLevel <= 1)
                            {
                                RemovePlayer(cl.Player);
                            }
                        }
                        else
                        {
                            cl.Player.IsInRvR = true;
                            countPlayer++;
                        }
                    }
                }

                if (!_isForcedOpen)
                {
                    if ((currentTime < _startTime || currentTime > _endTime) && !Close())
                        _regions.Foreach(id => WorldMgr.GetClientsOfRegion(id).Foreach(c => RemovePlayer(c, false)));
                }

                // check the Score every minutes and if the number of player is less than 8 pending 5 minutes stop count the point 
                if (checkScore == 0)
                {
                    // check if the number of players is sufficient to count points
                    if (countPlayer < Properties.RvR_NUMBER_OF_NEEDED_PLAYERS)
                        checkNumberOfPlayer++;
                    else
                        checkNumberOfPlayer = 0;

                    if (checkNumberOfPlayer < 5)
                    {
                        ActiveMaps().ForEach(map =>
                        {
                            var terr = map.Value.RvRTerritory;
                            if (terr != null && !string.IsNullOrEmpty(terr.Boss.GuildName) && Scores.ContainsKey(terr.Boss.GuildName))
                            {
                                if (map.Key.Contains("Debutant")) Scores[terr.Boss.GuildName] += 1;
                                else if (map.Key.Contains("Standard")) Scores[terr.Boss.GuildName] += 2;
                                else if (map.Key.Contains("Expert")) Scores[terr.Boss.GuildName] += 3;
                                else if (map.Key.Contains("Master")) Scores[terr.Boss.GuildName] += 4;
                            }
                        });
                    }
                }

                checkScore = (checkScore + 1) % 2;
            }

            if (currentTime.Date > RvRBonusDate)
            {
                ClearRvRBonus();
                RvRBonusDate = currentTime.Date;
                _currentMasterMap = null;
            }

            if (currentTime > _endTime)
            {
                _startTime = _startTime.AddHours(24D);
                _endTime = _endTime.AddHours(24D);
            }

            SaveScore();

            return _checkInterval;
        }

        private void SaveScore()
        {
            if (!Directory.Exists("temp"))
                Directory.CreateDirectory("temp");
            var lines = new string[]
            {
                DateTime.Now.ToString("o"),
                Scores.TryGetValue(ALBION, out var a) ? a.ToString() : "0",
                Scores.TryGetValue(HIBERNIA, out var h) ? h.ToString() : "0",
                Scores.TryGetValue(MIDGARD, out var m) ? m.ToString() : "0",
                winnerName ?? string.Empty,
                _currentMasterMap ?? string.Empty
            };
            File.WriteAllLines("temp/RvRScore.dat", lines);
        }

        private string ReadLastMasterMapFromFile()
        {
            try
            {
                var path = "temp/RvRScore.dat";
                if (!File.Exists(path)) return null;
                var lines = File.ReadAllLines(path);
                if (lines.Length >= 6)
                {
                    var val = (lines[5] ?? string.Empty).Trim();
                    return string.IsNullOrWhiteSpace(val) ? null : val;
                }
            }
            catch { /* ignore */ }
            return null;
        }

        private void ClearRvRBonus()
        {
            WorldMgr.GetClientsOfRealm(WinnerRealm).Foreach((client) =>
            {
                client.Player.BaseBuffBonusCategory[eProperty.MythicalCoin] -= 5;
                client.Player.BaseBuffBonusCategory[eProperty.XpPoints] -= 10;
                client.Player.BaseBuffBonusCategory[eProperty.RealmPoints] -= 5;
                client.Out.SendUpdatePlayer();
            });
            winnerName = "";
        }

        public bool Open(bool force)
        {
            _isForcedOpen = force;
            if (_isOpen)
                return true;
            _isOpen = true;

            _albion.RealmPoints = 0;
            _midgard.RealmPoints = 0;
            _hibernia.RealmPoints = 0;

            _albion.MeritPoints = 0;
            _midgard.MeritPoints = 0;
            _hibernia.MeritPoints = 0;

            _albion.BountyPoints = 0;
            _midgard.BountyPoints = 0;
            _hibernia.BountyPoints = 0;

            _albion.HasGuildBanner = false;
            _midgard.HasGuildBanner = false;
            _hibernia.HasGuildBanner = false;

            foreach (var zoneDict in holdingTimesByZone.Values)
                foreach (var realm in zoneDict.Keys.ToList())
                    zoneDict[realm] = TimeSpan.Zero;

            Scores[ALBION] = 0;
            Scores[HIBERNIA] = 0;
            Scores[MIDGARD] = 0;
            winnerName = string.Empty;

            string previousPick = _currentMasterMap ?? ReadLastMasterMapFromFile();
            SelectRandomMasterMap(avoidSame: true, previous: previousPick);

            SaveScore();
            RebuildActiveRegions();

            kills = new Dictionary<GamePlayer, short>();

            ActiveMaps().Foreach(m =>
            {
                if (m.Value.RvRTerritory != null)
                {
                    ((LordRvR)m.Value.RvRTerritory.Boss).StartRvR();
                    m.Value.RvRTerritory.Reset();
                    m.Value.RvRTerritory.ToggleBanner(false);
                }
            });

            log.Info($"RvRManager: Opened with Master map = {_currentMasterMap} (force={force})");
            return true;
        }

        private void SelectRandomMasterMap(bool avoidSame = false, string previous = null)
        {
            string old = previous ?? _currentMasterMap;
            string pick;

            if (avoidSame && old != null && MasterMapPrefixes.Length > 1)
            {
                do
                {
                    pick = MasterMapPrefixes[_rng.Next(MasterMapPrefixes.Length)];
                }
                while (pick == old);
            }
            else
            {
                pick = MasterMapPrefixes[_rng.Next(MasterMapPrefixes.Length)];
            }

            _currentMasterMap = pick;
            log.Info($"RvRManager: Selected Master map: {_currentMasterMap}");
        }

        private void RebuildActiveRegions()
        {
            _regions = ActiveMaps()
                .Select(kv => kv.Value.Position.RegionID)
                .Distinct()
                .OrderBy(id => id)
                .ToList();
        }

        /// <summary>
        /// Enumerates only the maps that are active for scoring/territory today:
        /// - Debutant / Standard / Expert (always on)
        /// - Master: only the selected prefix (Master01/02/03)
        /// </summary>
        private IEnumerable<KeyValuePair<string, RvRMap>> ActiveMaps()
        {
            foreach (var kv in _maps)
            {
                if (kv.Key.Contains("RvR-Debutant-") || kv.Key.Contains("RvR-Standard-") || kv.Key.Contains("RvR-Expert-"))
                    yield return kv;
            }

            if (!string.IsNullOrEmpty(_currentMasterMap))
            {
                string prefix = $"RvR-{_currentMasterMap}-";
                foreach (var kv in _maps)
                    if (kv.Key.StartsWith(prefix))
                        yield return kv;
            }
        }

        public bool Close()
        {
            if (!_isOpen)
                return false;
            _isOpen = false;
            _isForcedOpen = false;

            foreach (var zoneDict in holdingTimesByZone.Values)
                foreach (var realm in zoneDict.Keys.ToList())
                    zoneDict[realm] = TimeSpan.Zero;

            string messageScore = GetMessageScore();
            WorldMgr.GetAllPlayingClients().Foreach((c) =>
            {
                string message = LanguageMgr.GetTranslation(c, "RvrManager.Score.Title") + "\n";
                message += messageScore;
                if (string.IsNullOrEmpty(winnerName))
                    message += LanguageMgr.GetTranslation(c, "RvrManager.Score.NoWinner");
                else
                    message += LanguageMgr.GetTranslation(c, "RvrManager.Score.Winner") + ": " + winnerName;
                c.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_SystemWindow);
            });

            ActiveMaps().Select(m => m.Value).Where(m => m.RvRTerritory != null).Foreach(m =>
            {
                ((LordRvR)m.RvRTerritory.Boss).StopRvR();
                m.RvRTerritory.Reset();
            });

            this._maps.Values.GroupBy(v => v.Position.RegionID).ForEach(region =>
            {
                var characters = GameServer.Database.SelectObjects<DOLCharacters>(c => c.Region == +region.Key);
                foreach (DOLCharacters chr in characters)
                {
                    var client = WorldMgr.GetClientByPlayerID(chr.ObjectId, true, false);
                    if (client != null)
                    {
                        RemovePlayer(client);
                    }
                    else
                    {
                        RemovePlayer(chr);
                    }
                }
            });

            string message = string.Format("RvR Scores for the {0} :\n", DateTime.Now.Date.ToString("MM/dd/yyyy"));
            message += messageScore;
            if (string.IsNullOrEmpty(winnerName))
                message += "Winner: none";
            else
                message += "Winner: " + winnerName;

            short countKilledPlayers = 0;
            short maxKills = 0;
            string champion = "";
            foreach (KeyValuePair<GamePlayer, short> killsPerPlayer in Kills)
            {
                countKilledPlayers += killsPerPlayer.Value;
                if (killsPerPlayer.Value > maxKills)
                {
                    maxKills = killsPerPlayer.Value;
                    champion = killsPerPlayer.Key.Name;
                }
            }
            if (!string.IsNullOrEmpty(champion))
            {
                var championPlayer = Kills.FirstOrDefault(kvp => kvp.Key.Name == champion).Key;
                if (championPlayer != null)
                {
                    TaskManager.UpdateTaskProgress(championPlayer, "RvRChampionOfTheDay", 1);
                }
                NewsMgr.CreateNews("GameObjects.GamePlayer.RvR.Champion", 0, eNewsType.RvRGlobal, false, true, countKilledPlayers, champion, maxKills);
                message += string.Format(LanguageMgr.GetTranslation("EN", "GameObjects.GamePlayer.RvR.Champion", countKilledPlayers, champion, maxKills));
            }
            else
            {
                NewsMgr.CreateNews("GameObjects.GamePlayer.RvR", 0, eNewsType.RvRGlobal, false, true, countKilledPlayers);
                message += string.Format(LanguageMgr.GetTranslation("EN", "GameObjects.GamePlayer.RvR", countKilledPlayers));
            }

            if (Properties.DISCORD_ACTIVE)
            {
                var hook = new DolWebHook(Properties.DISCORD_WEBHOOK_ID);
                hook.SendMessage(message);
            }

            if (!string.IsNullOrEmpty(winnerName))
                ApplyRvRBonus();

            return true;
        }

        private void ApplyRvRBonus()
        {
            WorldMgr.GetClientsOfRealm(WinnerRealm).Foreach((client) =>
            {
                client.Player.BaseBuffBonusCategory[eProperty.MythicalCoin] += 5;
                client.Player.BaseBuffBonusCategory[eProperty.XpPoints] += 10;
                client.Player.BaseBuffBonusCategory[eProperty.RealmPoints] += 5;
                client.Out.SendUpdatePlayer();
            });
        }

        private string GetMessageScore()
        {
            string result = ALBION + ": " + Scores[ALBION] + " points\n";
            result += HIBERNIA + ": " + Scores[HIBERNIA] + " points\n";
            result += MIDGARD + ": " + Scores[MIDGARD] + " points\n";
            result += "\n";
            winnerName = "";
            int max = 0;
            foreach (KeyValuePair<string, int> score in Scores)
            {
                if (score.Value == max)
                    winnerName = "";
                else if (score.Value > max)
                {
                    max = score.Value;
                    winnerName = score.Key;
                }
            }

            return result;
        }

        public bool AddPlayer(GamePlayer player)
        {
            if (!_isOpen || player.Level < 20)
                return false;
            if (player.Client.Account.PrivLevel == (uint)ePrivLevel.GM)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RvRManager.GMNotAllowed"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }
            RvrPlayer rvr = new RvrPlayer(player, null);
            rvr.PvPSession = "RvR";
            GameServer.Database.AddObject(rvr);

            if (player.Guild != null)
            {
                player.RealGuild = player.Guild;
                player.Guild.RemovePlayer("RVR", player);
            }

            bool isAdded = false;

            if (player.Level < 20)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RvRManager.LowLevel"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return isAdded;
            }
            else
            {
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        _albion.AddPlayer(player);
                        isAdded = this.AddPlayerToCorrectZone(player, "ALB");
                        break;

                    case eRealm.Midgard:
                        _midgard.AddPlayer(player);
                        isAdded = this.AddPlayerToCorrectZone(player, "MID");
                        break;

                    case eRealm.Hibernia:
                        _hibernia.AddPlayer(player);
                        isAdded = this.AddPlayerToCorrectZone(player, "HIB");
                        break;
                }

                if (isAdded)
                {
                    player.IsInRvR = true;
                }

                if (player.Guild != null)
                    foreach (var i in player.Inventory.AllItems.Where(i => i.Emblem != 0))
                        i.Emblem = player.Guild.Emblem;

            }
            return isAdded;
        }

        private bool AddPlayerToCorrectZone(GamePlayer player, string realm)
        {
            string key = null;
            try
            {
                //if (player.Level >= 20 && player.Level < 26)
                //{
                //    key = "RvR-Novice-" + realm;
                //    if (!_maps.ContainsKey(key))
                //    {
                //        throw new KeyNotFoundException(key);
                //    }                 
                //}
                if (player.Level >= 20 && player.Level < 29)
                {
                    key = "RvR-Debutant-" + realm;
                    if (!_maps.ContainsKey(key))
                    {
                        throw new KeyNotFoundException(key);
                    }
                }
                else if (player.Level >= 29 && player.Level < 38)
                {
                    key = "RvR-Standard-" + realm;
                    if (!_maps.ContainsKey(key))
                    {
                        throw new KeyNotFoundException(key);
                    }
                }
                else if (player.Level >= 38 && player.Level < 46)
                {
                    key = "RvR-Expert-" + realm;
                    if (!_maps.ContainsKey(key))
                    {
                        throw new KeyNotFoundException(key);
                    }
                }
                else if (player.Level >= 46)
                {
                    if (string.IsNullOrEmpty(_currentMasterMap))
                        SelectRandomMasterMap();

                    key = $"RvR-{_currentMasterMap}-{realm}";
                    if (!_maps.ContainsKey(key)) throw new KeyNotFoundException(key);
                }
                //else if (player.Level >= 50 && player.IsRenaissance)
                //{
                //    //RvR DIVINITÉS acessible UNIQUEMENT aux joueurs « IsRenaissance » Level50
                //    key = "RvR-Divine-" + realm;
                //    if (!_maps.ContainsKey(key))
                //    {
                //        throw new KeyNotFoundException(key);
                //    }                 
                //}

                player.MoveTo(_maps[key!].Position);
                player.Bind(true);
                return true;
            }
            catch (KeyNotFoundException e)
            {
                log.Error(e.Message, e);
                return false;
            }
        }

        public void RemovePlayer(GameClient client, bool force = false)
        {
            if (client.Player != null)
                RemovePlayer(client.Player);
        }

        private void RemovePlayer(GamePlayer player, RvrPlayer rvrPlayer, bool force = false)
        {
            if (rvrPlayer == null)
            {
                if (player.Client.Account.PrivLevel <= 1)
                {
                    GameServer.Instance.Logger.Error("Player " + player.Name + " (" + player.InternalID + ") logged into RvR but RvR data was not found, potentially lost their guild info");
                }
                else if (!force) // By default, don't boot GMs
                {
                    return;
                }

                player.MoveTo(_stuckSpawn);
                if (player.Guild != null && player.Guild.GuildType == Guild.eGuildType.RvRGuild)
                    player.Guild.RemovePlayer("RVR", player);
                player.RealGuild = null;
                player.SaveIntoDatabase();
            }
            else
            {
                rvrPlayer.ResetCharacter(player);
                if (player.Client.Account.PrivLevel <= 1 || force)
                    player.MoveTo(Position.Create((ushort)rvrPlayer.OldRegion, rvrPlayer.OldX, rvrPlayer.OldY, rvrPlayer.OldZ, (ushort)rvrPlayer.OldHeading));
                if (player.Guild != null)
                    player.Guild.RemovePlayer("RVR", player);
                player.RealGuild = null;
                if (!string.IsNullOrWhiteSpace(rvrPlayer.GuildID))
                {
                    var guild = GuildMgr.GetGuildByGuildID(rvrPlayer.GuildID);
                    if (guild != null)
                    {
                        guild.AddPlayer(player, guild.GetRankByID(rvrPlayer.GuildRank), true);

                        foreach (var i in player.Inventory.AllItems.Where(i => i.Emblem != 0))
                            i.Emblem = guild.Emblem;
                    }
                }
                player.SaveIntoDatabase();
                rvrPlayer.PvPSession = "None";
                GameServer.Database.DeleteObject(rvrPlayer);
            }
        }

        public void RemovePlayer(GamePlayer player, bool force = false)
        {
            if (player.Client.Account.PrivLevel == (uint)ePrivLevel.GM)
                return;
            player.IsInRvR = false;
            var rvr = GameServer.Database.SelectObject<RvrPlayer>(r => r.PlayerID == player.InternalID);
            RemovePlayer(player, rvr);
        }

        public void RemovePlayer(DOLCharacters ch)
        {
            var rvr = GameServer.Database.SelectObject<RvrPlayer>(r => r.PlayerID == ch.ObjectId);
            if (rvr == null)
            {
                // AHHHHHHHHHHH
            }
            else
            {
                rvr.ResetCharacter(ch);
                GameServer.Database.SaveObject(ch);
                GameServer.Database.DeleteObject(rvr);
            }
        }


        private IList<string> _statCache = new List<string>();
        private DateTime _statLastCacheUpdate = DateTime.Now;

        public IList<string> GetStatistics(GamePlayer player)
        {
            var statList = new List<string>();

            if (!_isOpen)
            {
                if (WinnerRealm == player.Realm)
                {
                    statList.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "RvRManager.WinnerBonuses"));
                    statList.Add("");
                    statList.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "RvRManager.BonusGold"));
                    statList.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "RvRManager.BonusExperience"));
                    statList.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "RvRManager.BonusRealmPoints"));
                }
                else
                {
                    statList.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "RvRManager.LoserNoBonuses"));
                }
                return statList;
            }

            // Scores at the top
            statList.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "RvRManager.TotalScores"));
            statList.Add($"Albion: {Scores[ALBION]} points");
            statList.Add($"Midgard: {Scores[MIDGARD]} points");
            statList.Add($"Hibernia: {Scores[HIBERNIA]} points");
            statList.Add("");

            // Current Champion
            statList.Add("--------------------------------------------------------------");
            statList.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "RvRManager.CurrentChampion") + GetCurrentChampion(player.Client.Account.Language));
            statList.Add("--------------------------------------------------------------");
            statList.Add("");

            // Update statistics every 30 seconds
            if (DateTime.Now.Subtract(_statLastCacheUpdate) >= TimeSpan.FromSeconds(30))
            {
                _statLastCacheUpdate = DateTime.Now;
                _statCache.Clear();

                var zones = new List<string> { "Debutant", "Standard", "Expert" };
                if (!string.IsNullOrEmpty(_currentMasterMap))
                    zones.Add(_currentMasterMap);

                foreach (var zone in zones)
                {
                    string zoneDisplayName = zone switch
                    {
                        "Debutant" => "Debutant (lv 20 to 28)",
                        "Standard" => "Standard (lv 29 to 37)",
                        "Expert" => "Expert (lv 38 to 45)",
                        _ => $"Master (lv 46 to 50)"
                    };

                    _statCache.Add($"------------ RvR {zoneDisplayName} ------------");

                    foreach (var realm in new[] { eRealm.Albion, eRealm.Midgard, eRealm.Hibernia })
                    {
                        string realmShort = realm switch
                        {
                            eRealm.Albion => "ALB",
                            eRealm.Midgard => "MID",
                            eRealm.Hibernia => "HIB",
                            _ => "UNK"
                        };

                        string zoneKey = $"RvR-{zone}-{realmShort}";

                        if (!_maps.ContainsKey(zoneKey))
                        {
                            _statCache.Add($" - {GlobalConstants.RealmToName(realm)}: No data available");
                            continue;
                        }

                        var regionId = _maps[zoneKey].Position.RegionID;
                        var clients = WorldMgr.GetClientsOfRegion(regionId).Where(c => c.Player.Realm == realm);

                        _statCache.Add($" - {GlobalConstants.RealmToName(realm)}: {clients.Count()} {LanguageMgr.GetTranslation(player.Client.Account.Language, "RvRManager.Players")}, {clients.Sum(c => c.Player.Guild?.RealmPoints ?? 0)} {LanguageMgr.GetTranslation(player.Client.Account.Language, "RvRManager.RealmPoints")}");
                    }

                    _statCache.Add("");
                    _statCache.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "RvRManager.HoldingTime"));

                    foreach (var realm in new[] { eRealm.Albion, eRealm.Midgard, eRealm.Hibernia })
                    {
                        string realmShort = realm switch
                        {
                            eRealm.Albion => "ALB",
                            eRealm.Midgard => "MID",
                            eRealm.Hibernia => "HIB",
                            _ => "UNK"
                        };

                        var zoneKey = $"RvR-{zone}-{realmShort}";
                        TimeSpan holdingTime = holdingTimesByZone.ContainsKey(zoneKey) ? holdingTimesByZone[zoneKey][realm] : TimeSpan.Zero;
                        _statCache.Add($"   {GlobalConstants.RealmToName(realm)}: {Math.Round(holdingTime.TotalSeconds, 1)} seconds");
                    }

                    _statCache.Add("");
                }
            }

            statList.AddRange(_statCache);

            // Admin/GM Infos
            if (player.Client.Account.PrivLevel > 1)
            {
                statList.Add("");
                statList.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "RvRManager.Status") + (_isOpen ? " ouvert" : " fermé") + ".");
                statList.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "RvRManager.UpdateFrequency"));
                statList.Add($"Current Master Map: {_currentMasterMap}");
            }

            return statList;
        }

        private string GetCurrentChampion(string language)
        {
            string champion = "";
            short maxKills = 0;
            bool tie = false;

            foreach (KeyValuePair<GamePlayer, short> killsPerPlayer in Kills)
            {
                if (killsPerPlayer.Value > maxKills)
                {
                    maxKills = killsPerPlayer.Value;
                    champion = killsPerPlayer.Key.Name;
                    tie = false;
                }
                else if (killsPerPlayer.Value == maxKills)
                {
                    tie = true;
                }
            }

            return tie ? LanguageMgr.GetTranslation(language, "RvRManager.None") : champion;
        }

        public bool IsInRvr(GameLiving obj)
        {
            return IsOpen && obj != null && _regions.Any(id => id == obj.CurrentRegionID);
        }

        public bool IsAllowedToAttack(GameLiving attacker, GameLiving defender, bool quiet)
        {
            if (attacker.Realm == defender.Realm)
            {
                if (!quiet)
                    _MessageToLiving(attacker, LanguageMgr.GetTranslation((attacker as GamePlayer)?.Client, "RvRManager.CannotAttackRealmMember"), eChatType.CT_System);
                return false;
            }
            return true;
        }

        public bool IsRvRRegion(ushort id)
        {
            return _maps.Values.Any(v => v.Position.RegionID.Equals(id));
        }

        private static void _MessageToLiving(GameLiving living, string message, eChatType chatType)
        {
            if (living is GamePlayer)
                ((GamePlayer)living).Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }

    public class RvRMap
    {
        public RvRTerritory RvRTerritory { get; set; }
        public Position Position { get; set; }
    }
}
