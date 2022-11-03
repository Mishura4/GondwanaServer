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
using DOL.Database.Attributes;

namespace DOL.Database
{
    /// <summary>
    ///
    /// </summary>
    [DataTable(TableName = "DBTPPoint")]
    public class DBTPPoint : DataObject
    {
        protected ushort m_tpID = 0;
        protected int m_step;
        protected int m_x;
        protected int m_y;
        protected int m_z;
        protected ushort region;

        public DBTPPoint()
        {
        }

        public DBTPPoint(ushort region, int x, int y, int z)
        {
            m_x = x;
            m_y = y;
            m_z = z;
            this.region = region;
        }

        [DataElement(AllowDbNull = false, Index = true)]
        public ushort TPID
        {
            get { return m_tpID; }
            set { m_tpID = value; }
        }

        [DataElement(AllowDbNull = false)]
        public int Step
        {
            get { return m_step; }
            set { m_step = value; }
        }

        [DataElement(AllowDbNull = false)]
        public int X
        {
            get { return m_x; }
            set { m_x = value; }
        }

        [DataElement(AllowDbNull = false)]
        public int Y
        {
            get { return m_y; }
            set { m_y = value; }
        }

        [DataElement(AllowDbNull = false)]
        public int Z
        {
            get { return m_z; }
            set { m_z = value; }
        }

        [DataElement(AllowDbNull = false)]
        public ushort Region
        {
            get { return region; }
            set { region = value; }
        }
    }
}