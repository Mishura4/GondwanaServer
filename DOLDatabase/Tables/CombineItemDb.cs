using DOL.Database;
using DOL.Database.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOLDatabase.Tables
{
    [DataTable(TableName = "combineitem")]
    public class CombineItemDb
        : DataObject
    {
        private string m_itemsIds;
        private int m_spellEffect;
        private string m_itemTemplateId;
        private int m_craftingSkill;
        private int m_craftingValue;
        private int m_rewardCraftingSkills;
        private string m_areaId;
        private int m_chanceFailCombine;
        private int m_punishSpell;
        private int m_duration;
        private bool m_isUnique;
        private string m_combinexObjectModel;
        private bool m_applyRewardCraftingSkillsSystem;
        private string m_toolKit;
        private short m_ToolLoseDur;
        private string m_CombinationID;
        private bool m_allowVersion;

        [DataElement(AllowDbNull = false)]
        public string ItemsIds
        {
            get => m_itemsIds;

            set
            {
                m_itemsIds = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int SpellEffect
        {
            get => m_spellEffect;

            set
            {
                m_spellEffect = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string ItemTemplateId
        {
            get => m_itemTemplateId;

            set
            {
                m_itemTemplateId = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int CraftingSkill
        {
            get
            {
                return m_craftingSkill;
            }

            set
            {
                m_craftingSkill = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int CraftingValue
        {
            get
            {
                return m_craftingValue;
            }

            set
            {
                m_craftingValue = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public int RewardCraftingSkills
        {
            get
            {
                return m_rewardCraftingSkills;
            }

            set
            {
                Dirty = true;
                m_rewardCraftingSkills = value;
            }
        }

        /// <summary>
        /// If not null and player is not in the area, the combination can't success
        /// </summary>
        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string AreaId
        {
            get => m_areaId;

            set
            {
                Dirty = true;
                m_areaId = value;
            }
        }

        /// <summary>
        /// If the combination have the possibility to fail
        /// </summary>
        [DataElement(AllowDbNull = true)]
        public int ChanceFailCombine
        {
            get => m_chanceFailCombine;
            set
            {
                Dirty = true;
                m_chanceFailCombine = value;
            }
        }

        /// <summary>
        /// Punish Spell if fail
        /// </summary>
        [DataElement(AllowDbNull = true)]
        public int PunishSpell
        {
            get => m_punishSpell;
            set
            {
                Dirty = true;
                m_punishSpell = value;
            }
        }

        /// <summary>
        /// Time to combine
        /// </summary>
        [DataElement(AllowDbNull = true)]
        public int Duration
        {
            get => m_duration;
            set
            {
                Dirty = true;
                m_duration = value;
            }
        }

        /// <summary>
        /// Tool Models to Combine the Object
        /// </summary>
        [DataElement(AllowDbNull = true)]
        public string CombinexObjectModel
        {
            get => m_combinexObjectModel;
            set
            {
                Dirty = true;
                m_combinexObjectModel = value;
            }
        }

        /// <summary>
        /// Tool Models to Combine the Object
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public bool IsUnique
        {
            get => m_isUnique;
            set
            {
                Dirty = true;
                m_isUnique = value;
            }
        }

        /// <summary>
        /// Use the new system to calcuate points to ugrade crafting skills or not
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public bool ApplyRewardCraftingSkillsSystem
        {
            get => m_applyRewardCraftingSkillsSystem;
            set
            {
                Dirty = true;
                m_applyRewardCraftingSkillsSystem = value;
            }
        }

        /// <summary>
        /// Toolkit template to Combine the Object
        /// </summary>
        [DataElement(AllowDbNull = true)]
        public string ToolKit
        {
            get => m_toolKit;
            set
            {
                Dirty = true;
                m_toolKit = value;
            }
        }

        /// <summary>
        /// Point of durability lost per combination
        /// </summary>
        [DataElement(AllowDbNull = true)]
        public short ToolLoseDur
        {
            get => m_ToolLoseDur;
            set
            {
                Dirty = true;
                m_ToolLoseDur = value;
            }
        }

        /// <summary>
        /// Combination id to reference it in player list
        /// </summary>
        [DataElement(AllowDbNull = true)]
        public string CombinationId
        {
            get => m_CombinationID;
            set
            {
                Dirty = true;
                m_CombinationID = value;
            }
        }

        [DataElement(AllowDbNull = false)] // New property
        public bool AllowVersion
        {
            get => m_allowVersion;
            set
            {
                Dirty = true;
                m_allowVersion = value;
            }
        }
    }
}