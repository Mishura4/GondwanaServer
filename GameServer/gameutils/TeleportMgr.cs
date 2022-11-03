/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using log4net;

namespace DOL.GS
{
    public class TeleportMgr
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static Dictionary<ushort, DBTP> m_tpCache = new Dictionary<ushort, DBTP>();
        private static Dictionary<ushort, SortedList<int, DBTPPoint>> m_tppointCache = new Dictionary<ushort, SortedList<int, DBTPPoint>>();
        private static object LockObject = new object();
        /// <summary>
        /// Cache all the tppoints and tppointpoints
        /// </summary>
        private static void FillTPPointCache()
        {
            IList<DBTP> allTP = GameServer.Database.SelectAllObjects<DBTP>();
            foreach (DBTP tp in allTP)
            {
                m_tpCache.Add(tp.TPID, tp);
            }

            int duplicateCount = 0;

            IList<DBTPPoint> allTPPoint = GameServer.Database.SelectAllObjects<DBTPPoint>();
            foreach (DBTPPoint tpPoint in allTPPoint)
            {
                if (m_tppointCache.ContainsKey(tpPoint.TPID))
                {
                    if (m_tppointCache[tpPoint.TPID].ContainsKey(tpPoint.Step) == false)
                    {
                        m_tppointCache[tpPoint.TPID].Add(tpPoint.Step, tpPoint);
                    }
                    else
                    {
                        duplicateCount++;
                    }
                }
                else
                {
                    SortedList<int, DBTPPoint> pList = new SortedList<int, DBTPPoint>();
                    pList.Add(tpPoint.Step, tpPoint);
                    m_tppointCache.Add(tpPoint.TPID, pList);
                }
            }

            if (duplicateCount > 0)
            {
                log.ErrorFormat("{0} duplicate steps ignored while loading tppoints.", duplicateCount);
            }

            log.InfoFormat("TP cache filled with {0} tppoints.", m_tpCache.Count);
        }

        public static void UpdateTPInCache(ushort tpID)
        {
            log.DebugFormat("Updating tppoint {0} in tppoint cache.", tpID);

            DBTP dbtppoint = GameServer.Database.SelectObjects<DBTP>("`TPID` = @TPID", new QueryParameter("@TPID", tpID)).FirstOrDefault();
            if (dbtppoint != null)
            {
                if (m_tpCache.ContainsKey(tpID))
                {
                    m_tpCache[tpID] = dbtppoint;
                }
                else
                {
                    m_tpCache.Add(dbtppoint.TPID, dbtppoint);
                }
            }

            IList<DBTPPoint> tpPoints = GameServer.Database.SelectObjects<DBTPPoint>("`TPID` = @TPID", new QueryParameter("@TPID", tpID));
            SortedList<int, DBTPPoint> pList = new SortedList<int, DBTPPoint>();
            if (m_tppointCache.ContainsKey(tpID))
            {
                m_tppointCache[tpID] = pList;
            }
            else
            {
                m_tppointCache.Add(tpID, pList);
            }

            foreach (DBTPPoint tpPoint in tpPoints)
            {
                m_tppointCache[tpPoint.TPID].Add(tpPoint.Step, tpPoint);
            }
        }

        /// <summary>
        /// loads a tppoint from the cache
        /// </summary>
        /// <param name="tpID">tppoint to load</param>
        /// <returns>first tppointpoint of tppoint or null if not found</returns>
        public static TPPoint LoadTP(ushort tpID)
        {
            lock (LockObject)
            {
                if (m_tpCache.Count == 0)
                {
                    FillTPPointCache();
                }

                DBTP dbtppoint = null;

                if (m_tpCache.ContainsKey(tpID))
                {
                    dbtppoint = m_tpCache[tpID];
                }

                // even if tppoint entry not found see if tppointpoints exist and try to use it
                eTPPointType tppointType = eTPPointType.Random;

                if (dbtppoint != null)
                {
                    tppointType = (eTPPointType)dbtppoint.TPType;
                }

                SortedList<int, DBTPPoint> tppointPoints = null;

                if (m_tppointCache.ContainsKey(tpID))
                {
                    tppointPoints = m_tppointCache[tpID];
                }
                else
                {
                    tppointPoints = new SortedList<int, DBTPPoint>();
                }

                TPPoint prev = null;
                TPPoint first = null;

                foreach (DBTPPoint pp in tppointPoints.Values)
                {
                    TPPoint p = new TPPoint(pp.Region, pp.X, pp.Y, pp.Z, tppointType, pp);

                    if (first == null)
                    {
                        first = p;
                    }

                    p.Prev = prev;
                    if (prev != null)
                    {
                        prev.Next = p;
                    }

                    prev = p;
                }

                return first;
            }
        }

        /// <summary>
        /// Saves the tppoint into the database
        /// </summary>
        /// <param name="tpID">The tppoint ID</param>
        /// <param name="tppoint">The tppoint waypoint</param>
        public static void SaveTP(ushort tpID, TPPoint tppoint)
        {
            if (tppoint == null)
            {
                return;
            }

            // First delete any tppoint with this tpID from the database
            DBTP dbtp = GameServer.Database.SelectObjects<DBTP>("`TPID` = @TPID", new QueryParameter("@TPID", tpID)).FirstOrDefault();
            if (dbtp != null)
            {
                GameServer.Database.DeleteObject(dbtp);
            }

            GameServer.Database.DeleteObject(GameServer.Database.SelectObjects<DBTPPoint>("`TPID` = @TPID", new QueryParameter("@TPID", tpID)));

            // Now add this tppoint and iterate through the TPPoint linked list to add all the tppoint points
            TPPoint root = FindFirstTPPoint(tppoint);

            // Set the current tppointpoint to the rootpoint!
            tppoint = root;
            dbtp = new DBTP(tpID, root.Type);
            GameServer.Database.AddObject(dbtp);

            int i = 1;
            do
            {
                DBTPPoint dbpp = new DBTPPoint(tppoint.Region, tppoint.X, tppoint.Y, tppoint.Z);
                dbpp.Step = i++;
                dbpp.TPID = tpID;
                GameServer.Database.AddObject(dbpp);
                tppoint = tppoint.Next;
            }
            while (tppoint != null && tppoint != root);

            UpdateTPInCache(tpID);
        }

        /// <summary>
        /// Searches for the first point in the waypoints chain
        /// </summary>
        /// <param name="tppoint">One of the tppointpoints</param>
        /// <returns>The first tppointpoint in the chain or null</returns>
        public static TPPoint FindFirstTPPoint(TPPoint tppoint)
        {
            TPPoint root = tppoint;

            // avoid circularity
            int iteration = 50000;
            while (tppoint.Prev != null && tppoint.Prev != root)
            {
                tppoint = tppoint.Prev;
                iteration--;
                if (iteration <= 0)
                {
                    if (log.IsErrorEnabled)
                    {
                        log.Error("TP cannot be saved, it seems endless");
                    }

                    return null;
                }
            }

            return tppoint;
        }
    }
}