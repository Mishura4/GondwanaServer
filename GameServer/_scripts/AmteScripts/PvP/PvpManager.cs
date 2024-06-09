﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amte;
using AmteScripts.Utils;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Geometry;
using DOL.GS.PacketHandler;
using log4net;

namespace AmteScripts.Managers
{
    public class PvpManager
    {
        private static readonly TimeSpan _startTime = new TimeSpan(14, 0, 0);
        private static readonly TimeSpan _endTime = _startTime.Add(TimeSpan.FromHours(8));
        private const int _checkInterval = 30 * 1000; // 30 seconds
        private static readonly Position _stuckSpawn = Position.Create(51, 434303, 493165, 3088, 1069);

        #region Static part
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static PvpManager _instance;
        private static RegionTimer _timer;

        public static PvpManager Instance { get { return _instance; } }

        [ScriptLoadedEvent]
        public static void OnServerStarted(DOLEvent e, object sender, EventArgs args)
        {
            log.Info("PvP Manager: Started");
            _instance = new PvpManager();
            _timer = new RegionTimer(WorldMgr.GetRegion(1).TimeManager)
            {
                Callback = _instance._CheckPvP
            };
            _timer.Start(10000);
        }

        [ScriptUnloadedEvent]
        public static void OnServerStopped(DOLEvent e, object sender, EventArgs args)
        {
            log.Info("PvP Manager: Stopped");
            _timer.Stop();
        }
        #endregion

        private bool _isOpen;
        private bool _isForcedOpen;
        private ushort _region;

        public bool IsOpen { get { return _isOpen; } }
        public ushort Region { get { return _region; } }

        /// <summary>
        /// &lt;regionID, Tuple&lt;TPs, spawnAlb, spawnMid, spawnHib&gt;&gt;
        /// </summary>
        private readonly Dictionary<ushort, Tuple<GameNPC, Position>> _maps = new();

        private PvpManager()
        {
            FindPvPMaps();
        }

        public IEnumerable<ushort> Maps
        {
            get
            {
                return _maps.Keys;
            }
        }

        public IEnumerable<ushort> FindPvPMaps()
        {
            var npcs = WorldMgr.GetNPCsByGuild("PVP", eRealm.None);
            var maps = npcs.Select(n => n.CurrentRegionID).Distinct();

            _maps.Clear();
            foreach (var id in maps)
            {
                var spawn = WorldMgr.GetNPCsByNameFromRegion("SPAWN", id, eRealm.None).FirstOrDefault();
                if (spawn == null)
                    continue;
                _maps.Add(id, new Tuple<GameNPC, Position>(spawn, spawn.Position));
            }
            return (from m in _maps select m.Key);
        }

        private int _CheckPvP(RegionTimer callingtimer)
        {
            Console.WriteLine("Check PVP");
            if (!_isOpen)
            {
                _maps.Keys.Foreach(r => WorldMgr.GetClientsOfRegion(r).Foreach(RemovePlayer));
                if (DateTime.Now.TimeOfDay >= _startTime && DateTime.Now.TimeOfDay < _endTime)
                    Open(0, false);
            }
            else if (!_isForcedOpen && WorldMgr.GetClientsOfRegion(_region).Count < 3)
            {
                if ((DateTime.Now.TimeOfDay < _startTime || DateTime.Now.TimeOfDay > _endTime) && !Close())
                    WorldMgr.GetClientsOfRegion(_region).Foreach(RemovePlayer);
            }
            return _checkInterval;
        }

        public bool Open(ushort region, bool force)
        {
            _isForcedOpen = force;
            if (_isOpen)
                return true;
            _isOpen = true;
            if (region == 0 && _maps.Any())
                _region = _maps.Keys.ElementAt(Util.Random(_maps.Count - 1));
            else if (!_maps.ContainsKey(region))
                return false;
            else
                _region = region;
            return true;
        }

        public bool Close()
        {
            if (!_isOpen)
                return false;
            _isOpen = false;
            _isForcedOpen = false;

            WorldMgr.GetClientsOfRegion(_region).Where(player => player.Player != null).Foreach(RemovePlayer);
            GameServer.Database.SelectObjects<DOLCharacters>(c => c.Region == _region).Foreach(RemovePlayer);
            return true;
        }

        public bool AddPlayer(GamePlayer player)
        {
            if (!_isOpen || player.Level < 20)
                return false;
            if (player.Client.Account.PrivLevel == (uint)ePrivLevel.GM)
            {
                player.Out.SendMessage("Casse-toi connard de GM !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }
            RvrPlayer rvr = new RvrPlayer(player);
            GameServer.Database.AddObject(rvr);

            if (player.Guild != null)
                player.Guild.RemovePlayer("PVP", player);

            player.IsInPvP = true;
            player.MoveTo(_maps[_region].Item2);
            player.Bind(true);
            return true;
        }


        public void RemovePlayer(GameClient client)
        {
            if (client.Player != null)
                RemovePlayer(client.Player);
        }

        public void RemovePlayer(GamePlayer player)
        {
            if (player.Client.Account.PrivLevel == (uint)ePrivLevel.GM)
                return;
            var rvr = GameServer.Database.SelectObject<RvrPlayer>(r => r.PlayerID == player.InternalID);
            if (rvr == null)
            {
                player.IsInPvP = false;
                player.MoveTo(_stuckSpawn);
                player.SaveIntoDatabase();
            }
            else
            {
                rvr.ResetCharacter(player);
                player.MoveTo((ushort)rvr.OldRegion, rvr.OldX, rvr.OldY, rvr.OldZ, (ushort)rvr.OldHeading);
                if (player.Guild != null)
                    player.Guild.RemovePlayer("PVP", player);
                if (!string.IsNullOrWhiteSpace(rvr.GuildID))
                {
                    var guild = GuildMgr.GetGuildByGuildID(rvr.GuildID);
                    if (guild != null)
                        guild.AddPlayer(player, guild.GetRankByID(rvr.GuildRank));
                }
                player.IsInPvP = false;
                player.SaveIntoDatabase();
                GameServer.Database.DeleteObject(rvr);
            }
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

        public bool IsIn(GameLiving obj)
        {
            return IsOpen && obj != null && obj.CurrentRegionID == _region;
        }

        public bool IsPvPRegion(ushort id)
        {
            return _maps.ContainsKey(id);
        }
    }
}