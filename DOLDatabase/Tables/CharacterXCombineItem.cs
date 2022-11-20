using DOL.Database;
using DOL.Database.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOLDatabase.Tables
{
    [DataTable(TableName = "CharacterXCombineItem")]
    public class CharacterXCombineItem : DataObject
    {
        private string m_characterID;
        private string m_CombinationId;

        public CharacterXCombineItem()
        {
        }

        /// <summary>
        /// Create a new entry for this quest
        /// </summary>
        /// <param name="characterID"></param>
        /// <param name="dataQuestID"></param>
        public CharacterXCombineItem(string characterID, string combinationId)
        {
            m_characterID = characterID;
            m_CombinationId = combinationId;
        }

        /// <summary>
        /// DOLCharacters_ID of this player
        /// </summary>
        [DataElement(Varchar = 100, AllowDbNull = false, IndexColumns = "CombinationId")]
        public string Character_ID
        {
            get { return m_characterID; }
            set { m_characterID = value; Dirty = true; }
        }

        /// <summary>
        /// The ID of the DataQuest
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public string CombinationId
        {
            get { return m_CombinationId; }
            set { m_CombinationId = value; Dirty = true; }
        }
    }
}