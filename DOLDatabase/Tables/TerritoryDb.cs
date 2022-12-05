using DOL.Database;
using DOL.Database.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOLDatabase.Tables
{
    [DataTable(TableName = "territory")]
    public class TerritoryDb
        : DataObject
    {
        private ushort m_regionId;
        private ushort m_zoneId;
        private string m_areaId;
        private string m_bossMobId;
        private string m_groupId;
        private float m_areaX;
        private float m_areaY;
        private string m_bonus;
        private string m_guildOwner;
        private bool m_IsBannerSummoned;

        [DataElement(AllowDbNull = false)]
        public ushort RegionId
        {
            get
            {
                return m_regionId;
            }

            set
            {
                m_regionId = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public ushort ZoneId
        {
            get
            {
                return m_zoneId;
            }

            set
            {
                m_zoneId = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string AreaId
        {
            get
            {
                return m_areaId;
            }

            set
            {
                m_areaId = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string BossMobId
        {
            get
            {
                return m_bossMobId;
            }

            set
            {
                m_bossMobId = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string GuidldOwner
        {
            get
            {
                return m_guildOwner;
            }

            set
            {
                m_guildOwner = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string Bonus
        {
            get
            {
                return m_bonus;
            }

            set
            {
                m_bonus = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string GroupId
        {
            get
            {
                return m_groupId;
            }

            set
            {
                m_groupId = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public float AreaX
        {
            get
            {
                return m_areaX;
            }

            set
            {
                m_areaX = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public float AreaY
        {
            get
            {
                return m_areaY;
            }

            set
            {
                m_areaY = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public bool IsBannerSummoned
        {
            get => m_IsBannerSummoned;
            set
            {
                m_IsBannerSummoned = value;
                Dirty = true;
            }
        }
    }
}