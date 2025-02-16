using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using log4net;
using System.Reflection;
using DOL.GS;

namespace AmteScripts.Managers
{
    /// <summary>
    /// Loads the PvpSession records from DB table "PvPManager",
    /// picks a random session each day based on "Frequency" weighting,
    /// etc.
    /// </summary>
    public class PvpSessionMgr
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private static List<PvpSession> _allSessions = new List<PvpSession>();
        private static Random _rnd = new Random();

        private static List<ushort> _allZones = new();

        public static IReadOnlyList<ushort> AllZones => _allZones.AsReadOnly();
            
        /// <summary>
        /// Reload from DB
        /// </summary>
        public static void ReloadSessions()
        {
            _allSessions.Clear();
            _allZones.Clear();
            var sessions = GameServer.Database.SelectAllObjects<PvpSession>();
            _allSessions.AddRange(sessions);
            _allZones.AddRange(
                sessions.SelectMany(
                    s => s.ZoneList.Split(',')
                        .Select(s => s.Trim())
                        .Select(x => ushort.TryParse(x, out var parsed) ? parsed : (ushort?)null)
                        .Where(x => x.HasValue)
                        .Select(x => x.Value)
                ).Distinct()
            );
            log.InfoFormat("[PvpSessionMgr] Loaded {0} PvP sessions from DB (PvPManager table).", _allSessions.Count);
        }

        /// <summary>
        /// Return a random session, weighting by Frequency.
        /// Or null if none in DB.
        /// </summary>
        public static PvpSession PickRandomSession()
        {
            if (_allSessions.Count < 1) return null;

            // Weighted random by Frequency. 
            // Example approach:
            int totalWeight = _allSessions.Sum(s => s.Frequency);
            int roll = _rnd.Next(1, totalWeight + 1);
            int cumulative = 0;
            foreach (var s in _allSessions)
            {
                cumulative += s.Frequency;
                if (roll <= cumulative)
                {
                    return s;
                }
            }
            // fallback
            return _allSessions.Last();
        }

        /// <summary>
        /// Returns all loaded sessions, if you need enumerations
        /// </summary>
        public static IEnumerable<PvpSession> GetAllSessions()
        {
            return _allSessions;
        }
    }
}