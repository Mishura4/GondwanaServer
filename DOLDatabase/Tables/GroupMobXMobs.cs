using DOL.Database;
using DOL.Database.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOLDatabase.Tables
{
    [DataTable(TableName = "groupmobxmobs")]
    public class GroupMobXMobs
        : DataObject
    {
        private string m_groupId;
        private string m_MobId;
        private ushort m_regionID;

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

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string MobID
        {
            get
            {
                return m_MobId;
            }

            set
            {
                m_MobId = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public ushort RegionID
        {
            get
            {
                return m_regionID;
            }

            set
            {
                m_regionID = value;
                Dirty = true;
            }
        }
    }
}