using System;
using System.Reflection;
using DOL.Database;
using DOL.Database.Attributes;


namespace DOLDatabase.Tables
{
    [DataTable(TableName = "followingfriendmob")]
    public class followingfriendmob : DataObject
    {
        private string m_mobID;
        private string m_mobName;
        private string m_text;
        private string m_response;
        private string m_responseFollow;
        private string m_textUnfollow;
        private ushort m_followingFromRadius;
        private int m_aggroMultiplier;
        private string m_linkedGroupMob;
        private string m_areaToEnter;
        private int m_timerBeforeReset;

        [DataElement(AllowDbNull = false, Varchar = 255, Index = true)]
        public string MobID
        {
            get
            {
                return m_mobID;
            }

            set
            {
                m_mobID = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string MobName
        {
            get
            {
                return m_mobName;
            }

            set
            {
                m_mobName = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string Text
        {
            get
            {
                return m_text;
            }

            set
            {
                m_text = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string Response
        {
            get
            {
                return m_response;
            }

            set
            {
                m_response = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string ResponseFollow
        {
            get
            {
                return m_responseFollow;
            }

            set
            {
                m_responseFollow = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string TextUnfollow
        {
            get
            {
                return m_textUnfollow;
            }

            set
            {
                m_textUnfollow = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public ushort FollowingFromRadius
        {
            get
            {
                return m_followingFromRadius;
            }

            set
            {
                m_followingFromRadius = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public int AggroMultiplier
        {
            get
            {
                return m_aggroMultiplier;
            }

            set
            {
                m_aggroMultiplier = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string LinkedGroupMob
        {
            get
            {
                return m_linkedGroupMob;
            }

            set
            {
                m_linkedGroupMob = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string AreaToEnter
        {
            get
            {
                return m_areaToEnter;
            }

            set
            {
                m_areaToEnter = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public int TimerBeforeReset
        {
            get
            {
                return m_timerBeforeReset;
            }

            set
            {
                m_timerBeforeReset = value;
                Dirty = true;
            }
        }
    }
}