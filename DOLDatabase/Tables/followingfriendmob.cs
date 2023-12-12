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
        private string m_textIdle;
        private string m_textFollowing;
        private string m_reponseFollow;
        private string m_reponseUnfollow;
        private ushort m_followingFromRadius;
        private float m_aggroMultiplier = 1.0f;
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

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string TextIdle
        {
            get
            {
                return m_textIdle;
            }

            set
            {
                m_textIdle = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string TextFollowing
        {
            get
            {
                return m_textFollowing;
            }

            set
            {
                m_textFollowing = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string ReponseFollow
        {
            get
            {
                return m_reponseFollow;
            }

            set
            {
                m_reponseFollow = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string ReponseUnfollow
        {
            get
            {
                return m_reponseUnfollow;
            }

            set
            {
                m_reponseUnfollow = value;
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
        public float AggroMultiplier
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