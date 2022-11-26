using DOL.Database;
using DOL.Database.Attributes;


namespace DOLDatabase.Tables
{
    [DataTable(TableName = "Event")]
    public class EventDB : DataObject
    {
        private string m_eventName;
        private string m_eventAreas;
        private string m_eventZones;
        private bool m_showEvent;
        private int m_startConditionType;
        private int m_eventChance;
        private string m_DebutText;
        private string m_RandomText;
        private long m_RandTextInterval;
        private string m_remainingTimeText;
        private long m_remainingTimeInterval;
        private string m_endText;
        private int m_status;
        private int m_endingActionB;
        private int m_endingActionA;
        private long m_endTime;
        private long m_startedTime;
        private long m_eventChanceInterval;
        private string m_endingConditionTypes;
        private string m_mobNamesToKill;
        private string m_startActionStopEventID;
        private long m_startTriggerTime;
        private int m_timerType;
        private long chronoTime;
        private string endActionStartEventID;
        private string killStartingGroupMobId;
        private string m_resetEventId;
        private long m_chanceLastTimeChecked;
        private byte m_AnnonceType;
        private int m_Discord;

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string EventName
        {
            get
            {
                return m_eventName;
            }

            set
            {
                m_eventName = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string EventAreas
        {
            get
            {
                return m_eventAreas;
            }

            set
            {
                m_eventAreas = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string EventZones
        {
            get
            {
                return m_eventZones;
            }

            set
            {
                m_eventZones = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public bool ShowEvent
        {
            get
            {
                return m_showEvent;
            }

            set
            {
                m_showEvent = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int StartConditionType
        {
            get
            {
                return m_startConditionType;
            }

            set
            {
                m_startConditionType = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public long ChanceLastTimeChecked
        {
            get
            {
                return m_chanceLastTimeChecked;
            }

            set
            {
                m_chanceLastTimeChecked = value;
                Dirty = true;
            }
        }


        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string ResetEventId
        {
            get
            {
                return m_resetEventId;
            }

            set
            {
                m_resetEventId = value;
                Dirty = true;
            }
        }


        [DataElement(AllowDbNull = false)]
        public int TimerType
        {
            get
            {
                return m_timerType;
            }

            set
            {
                m_timerType = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public byte AnnonceType
        {
            get
            {
                return m_AnnonceType;
            }

            set
            {
                m_AnnonceType = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public long ChronoTime
        {
            get => chronoTime;

            set
            {
                chronoTime = value;
                Dirty = true;
            }
        }


        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string MobNamesToKill
        {
            get
            {
                return m_mobNamesToKill;
            }

            set
            {
                m_mobNamesToKill = value;
                Dirty = true;
            }
        }



        [DataElement(AllowDbNull = false, Varchar = 10)]
        public string EndingConditionTypes
        {
            get
            {
                return m_endingConditionTypes;
            }

            set
            {
                m_endingConditionTypes = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public int EventChance
        {
            get
            {
                return m_eventChance;
            }

            set
            {
                m_eventChance = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public long EventChanceInterval
        {
            get
            {
                return m_eventChanceInterval;
            }

            set
            {
                m_eventChanceInterval = value;
                Dirty = true;
            }
        }


        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string DebutText
        {
            get
            {
                return m_DebutText;
            }

            set
            {
                m_DebutText = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string RandomText
        {
            get
            {
                return m_RandomText;
            }

            set
            {
                m_RandomText = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public long RandTextInterval
        {
            get
            {
                return m_RandTextInterval;
            }

            set
            {
                m_RandTextInterval = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string RemainingTimeText
        {
            get
            {
                return m_remainingTimeText;
            }

            set
            {
                m_remainingTimeText = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public long RemainingTimeInterval
        {
            get
            {
                return m_remainingTimeInterval;
            }

            set
            {
                m_remainingTimeInterval = value;
                Dirty = true;
            }
        }


        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string EndText
        {
            get
            {
                return m_endText;
            }

            set
            {
                m_endText = value;
                Dirty = true;
            }
        }



        [DataElement(AllowDbNull = false)]
        public int Status
        {
            get
            {
                return m_status;
            }

            set
            {
                m_status = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int EndingActionA
        {
            get
            {
                return m_endingActionA;
            }

            set
            {
                m_endingActionA = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int EndingActionB
        {
            get
            {
                return m_endingActionB;
            }

            set
            {
                m_endingActionB = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public long EndTime
        {
            get
            {
                return m_endTime;
            }

            set
            {
                m_endTime = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string KillStartingGroupMobId
        {
            get => killStartingGroupMobId;

            set
            {
                killStartingGroupMobId = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string EndActionStartEventID
        {
            get => endActionStartEventID;

            set
            {
                endActionStartEventID = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string StartActionStopEventID
        {
            get
            {
                return m_startActionStopEventID;
            }

            set
            {
                m_startActionStopEventID = value;
                Dirty = true;
            }
        }


        [DataElement(AllowDbNull = false)]
        public long StartedTime
        {
            get
            {
                return m_startedTime;
            }

            set
            {
                m_startedTime = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public long StartTriggerTime
        {
            get
            {
                return m_startTriggerTime;
            }

            set
            {
                m_startTriggerTime = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int Discord
        {
            get
            {
                return m_Discord;
            }

            set
            {
                m_Discord = value;
                Dirty = true;
            }
        }
    }
}