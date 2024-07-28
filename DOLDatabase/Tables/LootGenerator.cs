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
    /// Database Storage of Tasks
    /// </summary>
    [DataTable(TableName = "LootGenerator")]
    public class LootGenerator : DataObject
    {
        /// <summary>
        /// Trigger Mob
        /// </summary>
        protected string m_mobName = string.Empty;
        /// <summary>
        /// Trigger Guild
        /// </summary>
        protected string m_mobGuild = string.Empty;
        /// <summary>
        /// Trigger Faction
        /// </summary>
        protected string m_mobFaction = string.Empty;
        /// <summary>
        /// Trigger Region
        /// </summary>
        protected string m_regionID = string.Empty;
        /// <summary>
        /// Trigger Mob Model
        /// </summary>
        protected string m_mobModel = string.Empty;
        /// <summary>
        /// Trigger Mob Bodytype
        /// </summary>
        protected string m_mobBodyType = string.Empty;
        /// <summary>
        /// Trigger Mob Race
        /// </summary>
        protected string m_mobRace = string.Empty;
        /// <summary>
        /// Class of the Loot Generator
        /// </summary>
        protected string m_lootGeneratorClass = string.Empty;
        /// <summary>
        /// IsRenaissance
        /// </summary>
        protected bool? m_isRenaissance = null;
        /// <summary>
        /// IsGoodReput
        /// </summary>
        protected bool? m_isGoodReput = null;
        protected bool? m_isBoss = null;
        protected bool m_condMustBeSetTogether = false;
        /// <summary>
        /// Exclusive Priority
        /// </summary>
        protected int m_exclusivePriority = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public LootGenerator()
        {
        }

        /// <summary>
        /// MobName
        /// </summary>
        [DataElement(AllowDbNull = true, Unique = false)]
        public String MobName
        {
            get { return m_mobName; }
            set
            {
                Dirty = true;
                m_mobName = value;
            }
        }

        /// <summary>
        /// MobGuild
        /// </summary>
        [DataElement(AllowDbNull = true, Unique = false)]
        public string MobGuild
        {
            get { return m_mobGuild; }
            set
            {
                Dirty = true;
                m_mobGuild = value;
            }
        }

        /// <summary>
        /// MobFaction
        /// </summary>
        [DataElement(AllowDbNull = true, Unique = false)]
        public string MobFaction
        {
            get { return m_mobFaction; }
            set
            {
                Dirty = true;
                m_mobFaction = value;
            }
        }

        /// <summary>
        /// Mobs Region ID
        /// </summary>
        [DataElement(AllowDbNull = false, Unique = false)]
        public string RegionID
        {
            get { return m_regionID; }
            set
            {
                Dirty = true;
                m_regionID = value;
            }
        }

        [DataElement(AllowDbNull = true, Unique = false)]
        public string MobModel
        {
            get { return m_mobModel; }
            set
            {
                Dirty = true;
                m_mobModel = value;
            }
        }

        [DataElement(AllowDbNull = true, Unique = false)]
        public string MobBodyType
        {
            get { return m_mobBodyType; }
            set
            {
                Dirty = true;
                m_mobBodyType = value;
            }
        }

        [DataElement(AllowDbNull = true, Unique = false)]
        public string MobRace
        {
            get { return m_mobRace; }
            set
            {
                Dirty = true;
                m_mobRace = value;
            }
        }

        [DataElement(AllowDbNull = true, Unique = false)]
        public bool? IsRenaissance
        {
            get { return m_isRenaissance; }
            set
            {
                Dirty = true;
                m_isRenaissance = value;
            }
        }

        [DataElement(AllowDbNull = true, Unique = false)]
        public bool? IsGoodReput
        {
            get { return m_isGoodReput; }
            set
            {
                Dirty = true;
                m_isGoodReput = value;
            }
        }

        [DataElement(AllowDbNull = true)]
        public bool? IsBoss
        {
            get { return m_isBoss; }
            set
            {
                Dirty = true;
                m_isBoss = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public bool CondMustBeSetTogether
        {
            get { return m_condMustBeSetTogether; }
            set
            {
                Dirty = true;
                m_condMustBeSetTogether = value;
            }
        }

        /// <summary>
        /// LootGeneratorClass
        /// </summary>
        [DataElement(AllowDbNull = false, Unique = false)]
        public string LootGeneratorClass
        {
            get { return m_lootGeneratorClass; }
            set
            {
                Dirty = true;
                m_lootGeneratorClass = value;
            }
        }

        /// <summary>
        /// ExclusivePriority
        /// </summary>
        [DataElement(AllowDbNull = false, Unique = false)]
        public int ExclusivePriority
        {
            get { return m_exclusivePriority; }
            set
            {
                Dirty = true;
                m_exclusivePriority = value;
            }
        }

    }
}
