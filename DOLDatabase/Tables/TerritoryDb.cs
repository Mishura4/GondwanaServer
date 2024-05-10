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
        private string m_areaIDs;
        private string m_name;
        private string m_bossMobId;
        private string m_groupId;
        private string m_bonus;
        private string m_ownerGuildID;
        private bool m_IsBannerSummoned;
        private int? m_portalX;
        private int? m_portalY;
        private int? m_portalZ;
        private int? m_guardTemplate;
        private int? m_healerTemplate;
        private int? m_mageTemplate;
        private int? m_archerTemplate;

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
        public string AreaIDs
        {
            get
            {
                return m_areaIDs;
            }

            set
            {
                m_areaIDs = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string Name
        {
            get
            {
                return m_name;
            }

            set
            {
                m_name = value;
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
        public string OwnerGuildID
        {
            get
            {
                return m_ownerGuildID;
            }

            set
            {
                m_ownerGuildID = value;
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
        public bool IsBannerSummoned
        {
            get => m_IsBannerSummoned;
            set
            {
                m_IsBannerSummoned = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public int? PortalX
        {
            get => m_portalX;
            set
            {
                m_portalX = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public int? PortalY
        {
            get => m_portalY;
            set
            {
                m_portalY = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public int? PortalZ
        {
            get => m_portalZ;
            set
            {
                m_portalZ = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public int? GuardNPCTemplate
        {
            get => m_guardTemplate;
            set
            {
                m_guardTemplate = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public int? HealerNPCTemplate
        {
            get => m_healerTemplate;
            set
            {
                m_healerTemplate = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public int? MageNPCTemplate
        {
            get => m_mageTemplate;
            set
            {
                m_mageTemplate = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public int? ArcherNPCTemplate
        {
            get => m_archerTemplate;
            set
            {
                m_archerTemplate = value;
                Dirty = true;
            }
        }
    }
}