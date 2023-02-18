using DOL.Database;
using DOL.Database.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOLDatabase.Tables
{
    [DataTable(TableName = "groupmob")]
    public class GroupMobDb
        : DataObject
    {
        private string m_groupId;
        private string m_IsInvincible;
        private int m_flag;
        private string m_visibleSlot;
        private string m_race;
        private string m_model;
        private string m_effect;
        private string m_InteractGroupId;
        private string m_groupMobInteractId;
        private string m_groupMobOrigin_FK_Id;
        private bool m_isQuestConditionFriendly;
        private string m_completedQuestNPCFlags;
        private ushort m_completedQuestNPCModel;
        private ushort m_completedQuestNPCSize;
        private ushort m_completedQuestAggro;
        private ushort m_completedQuestRange;
        private ushort m_completedStepQuestID;
        private int m_completedQuestId;
        private int m_completedQuestCount;

        [DataElement(AllowDbNull = false, Varchar = 255, Unique = true)]
        public string GroupId
        {
            get => m_groupId;
            set { m_groupId = value; Dirty = true; }
        }

        [DataElement(AllowDbNull = true, Varchar = 5)]
        public string IsInvincible
        {
            get => m_IsInvincible;
            set { Dirty = true; m_IsInvincible = value; }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public int Flag
        {
            get => m_flag;
            set { Dirty = true; m_flag = value; }
        }

        [DataElement(AllowDbNull = true, Varchar = 10)]
        public string VisibleSlot
        {
            get => m_visibleSlot;
            set { Dirty = true; m_visibleSlot = value; }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string Race
        {
            get => m_race;
            set { Dirty = true; m_race = value; }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string Model
        {
            get => m_model;
            set { Dirty = true; m_model = value; }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string Effect
        {
            get => m_effect;
            set { Dirty = true; m_effect = value; }
        }

        [DataElement(AllowDbNull = false)]
        public bool IsQuestConditionFriendly
        {
            get => m_isQuestConditionFriendly;
            set { Dirty = true; m_isQuestConditionFriendly = value; }
        }

        [DataElement(AllowDbNull = true)]
        public string CompletedQuestNPCFlags
        {
            get => m_completedQuestNPCFlags;
            set { Dirty = true; m_completedQuestNPCFlags = value; }
        }

        [DataElement(AllowDbNull = false)]
        public ushort CompletedQuestNPCModel
        {
            get => m_completedQuestNPCModel;
            set { Dirty = true; m_completedQuestNPCModel = value; }
        }

        [DataElement(AllowDbNull = false)]
        public ushort CompletedQuestNPCSize
        {
            get => m_completedQuestNPCSize;
            set { Dirty = true; m_completedQuestNPCSize = value; }
        }
        [DataElement(AllowDbNull = false)]
        public ushort CompletedQuestAggro
        {
            get => m_completedQuestAggro;
            set { Dirty = true; m_completedQuestAggro = value; }
        }
        [DataElement(AllowDbNull = false)]
        public ushort CompletedQuestRange
        {
            get => m_completedQuestRange;
            set { Dirty = true; m_completedQuestRange = value; }
        }


        [DataElement(AllowDbNull = false)]
        public ushort CompletedStepQuestID
        {
            get => m_completedStepQuestID;
            set { Dirty = true; m_completedStepQuestID = value; }
        }

        [DataElement(AllowDbNull = false)]
        public int CompletedQuestID
        {
            get => m_completedQuestId;
            set { Dirty = true; m_completedQuestId = value; }
        }

        [DataElement(AllowDbNull = false)]
        public int CompletedQuestCount
        {
            get => m_completedQuestCount;
            set { Dirty = true; m_completedQuestCount = value; }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string SlaveGroupId
        {
            get => m_InteractGroupId;
            set { m_InteractGroupId = value; Dirty = true; }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string GroupMobInteract_FK_Id
        {
            get => m_groupMobInteractId;
            set { Dirty = true; m_groupMobInteractId = value; }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string GroupMobOrigin_FK_Id
        {
            get => m_groupMobOrigin_FK_Id;
            set { Dirty = true; m_groupMobOrigin_FK_Id = value; }
        }
    }
}