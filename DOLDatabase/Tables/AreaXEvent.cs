using System.Timers;
using DOL.Database;
using DOL.Database.Attributes;

namespace DOLDatabase.Tables
{
    [DataTable(TableName = "AreaXEvent")]
    public class AreaXEvent
        : DataObject
    {
        private string m_eventID;
        private string m_areaID;
        private int? m_playersNb;
        private string? m_mobs;
        private string? m_useItem;
        private string? m_whisper;
        private bool? m_playersLeave;
        private bool? m_resetEvent;
        private int m_timerCount;

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

        [DataElement(AllowDbNull = false, Varchar = 255, Index = true)]
        public string AreaID
        {
            get
            {
                return m_areaID;
            }

            set
            {
                m_areaID = value;
                Dirty = true;
            }
        }


        [DataElement(AllowDbNull = true, Varchar = 255)]
        public int? PlayersNb
        {
            get
            {
                return m_playersNb;
            }

            set
            {
                m_playersNb = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string? Mobs
        {
            get
            {
                return m_mobs;
            }

            set
            {
                m_mobs = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string? UseItem
        {
            get
            {
                return m_useItem;
            }

            set
            {
                m_useItem = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string? Whisper
        {
            get
            {
                return m_whisper;
            }

            set
            {
                m_whisper = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public bool? PlayersLeave
        {
            get
            {
                return m_playersLeave;
            }

            set
            {
                m_playersLeave = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public bool? ResetEvent
        {
            get
            {
                return m_resetEvent;
            }

            set
            {
                m_resetEvent = value;
                Dirty = true;
            }
        }
        [DataElement(AllowDbNull = false, Varchar = 255)]
        public int TimerCount
        {
            get
            {
                return m_timerCount;
            }

            set
            {
                m_timerCount = value;
                Dirty = true;
            }
        }
    }
}