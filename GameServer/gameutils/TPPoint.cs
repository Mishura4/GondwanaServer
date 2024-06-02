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
using System;
using System.Linq;
using System.Collections.Generic;
using DOL.Database;
using DOL.Geometry;
using DOL.GS.Geometry;

namespace DOL.GS
{
    /// <summary>
    /// represents a point in a way path
    /// </summary>
    public class TPPoint
    {
        public Position Position
        {
            get;
            init;
        } = Position.Nowhere;
        
        protected TPPoint m_next = null;
        protected TPPoint m_prev = null;
        protected eTPPointType m_type;
        private DBTPPoint dbTPPoint;
        protected bool m_flag;
        private ushort region;

        protected const ushort PLAYERS_RADIUS = 1500;

        public TPPoint(TPPoint pp) : this(pp.Position, pp.Type) { }

        public TPPoint(Position p, eTPPointType type) : this(p.RegionID, (int)p.X, (int)p.Y, (int)p.Z, type, new DBTPPoint(p.RegionID, (int)p.X, (int)p.Y, (int)p.Z)) { }

        public TPPoint(ushort region, int x, int y, int z, eTPPointType type, DBTPPoint bTPPoint)
        {
            Position = Position.Create(region, x, y, z);
            m_type = type;
            m_flag = false;
            dbTPPoint = bTPPoint;
            this.region = region;
        }

        /// <summary>
        /// next waypoint in path
        /// </summary>
        public TPPoint Next
        {
            get { return m_next; }
            set { m_next = value; }
        }

        /// <summary>
        /// previous waypoint in path
        /// </summary>
        public TPPoint Prev
        {
            get { return m_prev; }
            set { m_prev = value; }
        }

        /// <summary>
        /// flag toggle when go through pathpoint
        /// </summary>
        public bool FiredFlag
        {
            get { return m_flag; }
            set { m_flag = value; }
        }

        /// <summary>
        /// path type
        /// </summary>
        public eTPPointType Type
        {
            get { return m_type; }
            set { m_type = value; }
        }

        public DBTPPoint DbTPPoint { get => dbTPPoint; set => dbTPPoint = value; }
        public ushort Region { get => region; set => region = value; }

        public TPPoint GetNextTPPoint()
        {
            TPPoint next = null;
            IList<DBTPPoint> tpPoints = GameServer.Database.SelectObjects<DBTPPoint>(DB.Column("TPID").IsEqualTo(dbTPPoint.TPID));
            DBTP dbtp = GameServer.Database.SelectObjects<DBTP>(DB.Column("TPID").IsEqualTo(dbTPPoint.TPID)).FirstOrDefault();
            DBTPPoint randomTPPoint = tpPoints[Util.Random(tpPoints.Count - 1)];
            switch (Type)
            {
                case eTPPointType.Loop:
                    if (m_next != null)
                        next = m_next;
                    else
                        next = TeleportMgr.FindFirstTPPoint(this);
                    break;
                case eTPPointType.Smart:
                    next = GetSmarttNextPoint();
                    if (next == null)
                    {
                        next = new TPPoint(randomTPPoint.Region, randomTPPoint.X, randomTPPoint.Y, randomTPPoint.Z, (eTPPointType)dbtp.TPType, randomTPPoint);
                    }
                    break;
                case eTPPointType.Random:

                    next = new TPPoint(randomTPPoint.Region, randomTPPoint.X, randomTPPoint.Y, randomTPPoint.Z, (eTPPointType)dbtp.TPType, randomTPPoint);
                    break;
            }

            return next;
        }

        public TPPoint GetSmarttNextPoint()
        {
            TPPoint nearest = null;

            int countPlayer = 0;
            var pp = TeleportMgr.FindFirstTPPoint(this);
            while (pp.Next != null)
            {
                if (pp != this)
                {
                    int newCount = WorldMgr.GetPlayersCloseToSpot(pp.Region, pp.Position.X, pp.Position.Y, pp.Position.Z, PLAYERS_RADIUS).OfType<GamePlayer>().Count();
                    if (newCount > countPlayer)
                    {
                        nearest = pp;
                        countPlayer = newCount;
                    }
                }
                pp = pp.Next;
            }

            return nearest;
        }
    }
}