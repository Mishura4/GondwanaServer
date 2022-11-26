using DOL.Database;
using DOL.Database.Attributes;

namespace DOLDatabase.Tables
{
    [DataTable(TableName = "EventsXObjects")]
    public class EventsXObjects
        : DataObject
    {
        private string m_itemID;
        private bool m_canRespawn;
        private int m_startEffect;
        private int m_endEffect;
        private bool m_isCoffre;
        private bool m_isMob;
        private string m_eventID;
        private ushort m_region;
        private string m_name;
        private int m_experienceFactor;

        [DataElement(AllowDbNull = false, Varchar = 255, Index = true)]
        public string EventID
        {
            get
            {
                return m_eventID;
            }

            set
            {
                m_eventID = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string ItemID
        {
            get
            {
                return m_itemID;
            }

            set
            {
                m_itemID = value;
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

        [DataElement(AllowDbNull = false)]
        public bool CanRespawn
        {
            get
            {
                return m_canRespawn;
            }

            set
            {
                m_canRespawn = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public bool IsCoffre
        {
            get
            {
                return m_isCoffre;
            }

            set
            {
                m_isCoffre = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public bool IsMob
        {
            get
            {
                return m_isMob;
            }

            set
            {
                m_isMob = value;
                Dirty = true;
            }
        }


        [DataElement(AllowDbNull = true)]
        public int StartEffect
        {
            get
            {
                return m_startEffect;
            }

            set
            {
                m_startEffect = value;
                Dirty = true;
            }
        }


        [DataElement(AllowDbNull = false)]
        public int EndEffect
        {
            get
            {
                return m_endEffect;
            }

            set
            {
                m_endEffect = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public ushort Region
        {
            get
            {
                return m_region;
            }

            set
            {
                m_region = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int ExperienceFactor
        {
            get => m_experienceFactor;
            set { Dirty = true; m_experienceFactor = value; }
        }
    }
}