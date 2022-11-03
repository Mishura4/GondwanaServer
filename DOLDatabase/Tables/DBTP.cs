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
    public enum eTPPointType : int
    {
        Random = 1,
        Loop = 2,
        Smart = 3,
    }

    /// <summary>
    ///
    /// </summary>
    [DataTable(TableName = "DBTP")]
    public class DBTP : DataObject
    {
        protected ushort m_region = 0;
        protected ushort m_tpID = 0;
        protected int m_type;// etype

        public DBTP()
        {
        }

        public DBTP(ushort tppointid, eTPPointType type)
        {
            m_tpID = tppointid;
            m_type = (int)type;
        }

        [DataElement(AllowDbNull = false, Unique = true)]
        public ushort TPID
        {
            get { return m_tpID; }
            set { m_tpID = value; }
        }

        [DataElement(AllowDbNull = false)]
        public int TPType
        {
            get { return m_type; }
            set { m_type = value; }
        }

        [DataElement(AllowDbNull = true)]
        public ushort RegionID
        {
            get { return m_region; }
            set { m_region = value; }
        }
    }
}