
using DOL.Database.Attributes;


namespace DOL.Database
{
    /// <summary>
    /// TaskXPlayer
    /// </summary>
    [DataTable(TableName = "TaskXPlayer")]
    public class TaskXPlayer : DataObject
    {
        private string m_playerName;
        private string m_killEnemyPlayersGroup = "0|0";
        private string m_killEnemyPlayersAlone = "0|0";
        private string m_killKeepGuards = "0|0";
        private string m_takeKeeps = "0|0";
        private string m_rvRChampionOfTheDay = "0|0";
        private string m_killTerritoryGuards = "0|0";
        private string m_killTerritoryBoss = "0|0";
        private string m_turnInPvPGvGTaskToken = "0|0";
        private string m_killCreaturesInDungeons = "0|0";
        private string m_killOutdoorsCreatures = "0|0";
        private string m_killAnimalCreatures = "0|0";
        private string m_killDemonCreatures = "0|0";
        private string m_killDragonCreatures = "0|0";
        private string m_killElementalCreatures = "0|0";
        private string m_killGiantCreatures = "0|0";
        private string m_killHumanoidCreatures = "0|0";
        private string m_killInsectCreatures = "0|0";
        private string m_killMagicalCreatures = "0|0";
        private string m_killReptileCreatures = "0|0";
        private string m_killPlantCreatures = "0|0";
        private string m_killUndeadCreatures = "0|0";
        private string m_turnInPvETaskToken = "0|0";
        private string m_successfulItemCombinations = "0|0";
        private string m_masteredCrafts = "0|0";
        private string m_masterpieceCrafted = "0|0";
        private string m_turnInCraftingTaskToken = "0|0";
        private string m_epicBossesSlaughtered = "0|0";
        private string m_itemsSoldToPlayers = "0|0";
        private string m_successfulPvPThefts = "0|0";
        private string m_outlawPlayersSentToJail = "0|0";
        private string m_enemiesKilledInAdrenalineMode = "0|0";
        private string m_questsCompleted = "0|0";

        [DataElement(AllowDbNull = false)]
        public string PlayerName
        {
            get { return m_playerName; }
            set
            {
                Dirty = true;
                m_playerName = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillEnemyPlayersGroup
        {
            get { return m_killEnemyPlayersGroup; }
            set
            {
                Dirty = true;
                m_killEnemyPlayersGroup = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillEnemyPlayersAlone
        {
            get { return m_killEnemyPlayersAlone; }
            set
            {
                Dirty = true;
                m_killEnemyPlayersAlone = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillKeepGuards
        {
            get { return m_killKeepGuards; }
            set
            {
                Dirty = true;
                m_killKeepGuards = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string TakeKeeps
        {
            get { return m_takeKeeps; }
            set
            {
                Dirty = true;
                m_takeKeeps = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string RvRChampionOfTheDay
        {
            get { return m_rvRChampionOfTheDay; }
            set
            {
                Dirty = true;
                m_rvRChampionOfTheDay = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillTerritoryGuards
        {
            get { return m_killTerritoryGuards; }
            set
            {
                Dirty = true;
                m_killTerritoryGuards = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillTerritoryBoss
        {
            get { return m_killTerritoryBoss; }
            set
            {
                Dirty = true;
                m_killTerritoryBoss = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string TurnInPvPGvGTaskToken
        {
            get { return m_turnInPvPGvGTaskToken; }
            set
            {
                Dirty = true;
                m_turnInPvPGvGTaskToken = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillCreaturesInDungeons
        {
            get { return m_killCreaturesInDungeons; }
            set
            {
                Dirty = true;
                m_killCreaturesInDungeons = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillOutdoorsCreatures
        {
            get { return m_killOutdoorsCreatures; }
            set
            {
                Dirty = true;
                m_killOutdoorsCreatures = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillAnimalCreatures
        {
            get { return m_killAnimalCreatures; }
            set
            {
                Dirty = true;
                m_killAnimalCreatures = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillDemonCreatures
        {
            get { return m_killDemonCreatures; }
            set
            {
                Dirty = true;
                m_killDemonCreatures = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillDragonCreatures
        {
            get { return m_killDragonCreatures; }
            set
            {
                Dirty = true;
                m_killDragonCreatures = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillElementalCreatures
        {
            get { return m_killElementalCreatures; }
            set
            {
                Dirty = true;
                m_killElementalCreatures = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillGiantCreatures
        {
            get { return m_killGiantCreatures; }
            set
            {
                Dirty = true;
                m_killGiantCreatures = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillHumanoidCreatures
        {
            get { return m_killHumanoidCreatures; }
            set
            {
                Dirty = true;
                m_killHumanoidCreatures = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillInsectCreatures
        {
            get { return m_killInsectCreatures; }
            set
            {
                Dirty = true;
                m_killInsectCreatures = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillMagicalCreatures
        {
            get { return m_killMagicalCreatures; }
            set
            {
                Dirty = true;
                m_killMagicalCreatures = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillReptileCreatures
        {
            get { return m_killReptileCreatures; }
            set
            {
                Dirty = true;
                m_killReptileCreatures = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillPlantCreatures
        {
            get { return m_killPlantCreatures; }
            set
            {
                Dirty = true;
                m_killPlantCreatures = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string KillUndeadCreatures
        {
            get { return m_killUndeadCreatures; }
            set
            {
                Dirty = true;
                m_killUndeadCreatures = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string TurnInPvETaskToken
        {
            get { return m_turnInPvETaskToken; }
            set
            {
                Dirty = true;
                m_turnInPvETaskToken = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string SuccessfulItemCombinations
        {
            get { return m_successfulItemCombinations; }
            set
            {
                Dirty = true;
                m_successfulItemCombinations = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string MasteredCrafts
        {
            get { return m_masteredCrafts; }
            set
            {
                Dirty = true;
                m_masteredCrafts = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string MasterpieceCrafted
        {
            get { return m_masterpieceCrafted; }
            set
            {
                Dirty = true;
                m_masterpieceCrafted = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string TurnInCraftingTaskToken
        {
            get { return m_turnInCraftingTaskToken; }
            set
            {
                Dirty = true;
                m_turnInCraftingTaskToken = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string EpicBossesSlaughtered
        {
            get { return m_epicBossesSlaughtered; }
            set
            {
                Dirty = true;
                m_epicBossesSlaughtered = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string ItemsSoldToPlayers
        {
            get { return m_itemsSoldToPlayers; }
            set
            {
                Dirty = true;
                m_itemsSoldToPlayers = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string SuccessfulPvPThefts
        {
            get { return m_successfulPvPThefts; }
            set
            {
                Dirty = true;
                m_successfulPvPThefts = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string OutlawPlayersSentToJail
        {
            get { return m_outlawPlayersSentToJail; }
            set
            {
                Dirty = true;
                m_outlawPlayersSentToJail = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string EnemiesKilledInAdrenalineMode
        {
            get { return m_enemiesKilledInAdrenalineMode; }
            set
            {
                Dirty = true;
                m_enemiesKilledInAdrenalineMode = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string QuestsCompleted
        {
            get { return m_questsCompleted; }
            set
            {
                Dirty = true;
                m_questsCompleted = value;
            }
        }
    }
}