/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.AI.Brain;
using System.Linq;
using DOL.GS.Quests;
using DOL.GameEvents;

namespace DOL.GS
{
    /// <summary>
    /// LootGeneratorMobTemplate
    /// This implementation uses LootTemplates to relate loots to a specific mob type.
    /// Used DB Tables: 
    ///				MobDropTemplate  (Relation between Mob and loottemplate
    ///				DropTemplateXItemTemplate	(loottemplate containing possible loot items)
    /// </summary>
    public class LootGeneratorMobTemplate : LootGeneratorBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Map holding a list of ItemTemplateIDs for each TemplateName
        /// 1:n mapping between loottemplateName and loottemplate entries
        /// </summary>
        protected static Dictionary<string, Dictionary<string, DropTemplateXItemTemplate>> m_lootTemplates;

        /// <summary>
        /// Map holding the corresponding LootTemplateName for each MobName
        /// 1:n Mapping between Mob and LootTemplate
        /// </summary>
        protected static Dictionary<string, List<MobDropTemplate>> m_mobXLootTemplates;

        /// <summary>
        /// Construct a new templategenerate and load its values from database.
        /// </summary>
        public LootGeneratorMobTemplate()
        {
            PreloadLootTemplates();
        }

        public static void ReloadLootTemplates()
        {
            m_lootTemplates = null;
            m_mobXLootTemplates = null;
            PreloadLootTemplates();
        }

        /// <summary>
        /// Loads the DropTemplateXItemTemplate
        /// </summary>
        /// <returns></returns>
        protected static bool PreloadLootTemplates()
        {
            if (m_lootTemplates == null)
            {
                m_lootTemplates = new Dictionary<string, Dictionary<string, DropTemplateXItemTemplate>>();

                lock (m_lootTemplates)
                {
                    IList<DropTemplateXItemTemplate> dbLootTemplates;

                    try
                    {
                        // TemplateName (typically the mob name), ItemTemplateID, Chance
                        dbLootTemplates = GameServer.Database.SelectAllObjects<DropTemplateXItemTemplate>();
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                        {
                            log.Error("LootGeneratorMobTemplate: DropTemplateXItemTemplate could not be loaded:", e);
                        }
                        return false;
                    }

                    if (dbLootTemplates != null)
                    {
                        Dictionary<string, DropTemplateXItemTemplate> loot;

                        foreach (DropTemplateXItemTemplate dbTemplate in dbLootTemplates)
                        {
                            if (!m_lootTemplates.TryGetValue(dbTemplate.TemplateName.ToLower(), out loot))
                            {
                                loot = new Dictionary<string, DropTemplateXItemTemplate>();
                                m_lootTemplates[dbTemplate.TemplateName.ToLower()] = loot;
                            }

                            ItemTemplate drop = GameServer.Database.FindObjectByKey<ItemTemplate>(dbTemplate.ItemTemplateID);

                            if (drop == null)
                            {
                                if (log.IsErrorEnabled)
                                    log.Error("ItemTemplate: " + dbTemplate.ItemTemplateID + " is not found, it is referenced from DropTemplateXItemTemplate: " + dbTemplate.TemplateName);
                            }
                            else
                            {
                                if (!loot.ContainsKey(dbTemplate.ItemTemplateID.ToLower()))
                                    loot.Add(dbTemplate.ItemTemplateID.ToLower(), dbTemplate);
                            }
                        }
                    }
                }

                log.Info("DropTemplateXItemTemplates pre-loaded.");
            }

            if (m_mobXLootTemplates == null)
            {
                m_mobXLootTemplates = new Dictionary<string, List<MobDropTemplate>>();

                lock (m_mobXLootTemplates)
                {
                    IList<MobDropTemplate> dbMobXLootTemplates;

                    try
                    {
                        // MobName, LootTemplateName, DropCount
                        dbMobXLootTemplates = GameServer.Database.SelectAllObjects<MobDropTemplate>();
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                        {
                            log.Error("LootGeneratorMobTemplate: MobDropTemplate could not be loaded", e);
                        }
                        return false;
                    }

                    if (dbMobXLootTemplates != null)
                    {
                        foreach (MobDropTemplate dbMobXTemplate in dbMobXLootTemplates)
                        {
                            // There can be multiple MobDropTemplate for a mob, each pointing to a different loot template
                            List<MobDropTemplate> mobxLootTemplates;
                            if (!m_mobXLootTemplates.TryGetValue(dbMobXTemplate.MobName.ToLower(), out mobxLootTemplates))
                            {
                                mobxLootTemplates = new List<MobDropTemplate>();
                                m_mobXLootTemplates[dbMobXTemplate.MobName.ToLower()] = mobxLootTemplates;
                            }
                            mobxLootTemplates.Add(dbMobXTemplate);
                        }
                    }
                }

                log.Info("MobDropTemplates pre-loaded.");
            }

            return true;
        }

        /// <summary>
        /// Reload the loot templates for this mob
        /// </summary>
        /// <param name="mob"></param>
        public override void Refresh(GameNPC mob)
        {
            if (mob == null)
                return;

            bool isDefaultLootTemplateRefreshed = false;

            // First see if there are any MobXLootTemplates associated with this mob
            IList<MobDropTemplate> mxlts = DOLDB<MobDropTemplate>.SelectObjects(DB.Column(nameof(MobDropTemplate.MobName)).IsEqualTo(mob.Name));

            if (mxlts != null && mxlts.Count > 0)
            {
                lock (m_mobXLootTemplates)
                {
                    foreach (MobDropTemplate mxlt in mxlts)
                        m_mobXLootTemplates.Remove(mxlt.MobName.ToLower());
                    foreach (MobDropTemplate mxlt in mxlts)
                    {
                        List<MobDropTemplate> mobxLootTemplates;
                        if (!m_mobXLootTemplates.TryGetValue(mxlt.MobName.ToLower(), out mobxLootTemplates))
                        {
                            mobxLootTemplates = new List<MobDropTemplate>();
                            m_mobXLootTemplates[mxlt.MobName.ToLower()] = mobxLootTemplates;
                        }
                        mobxLootTemplates.Add(mxlt);

                        RefreshLootTemplate(mxlt.LootTemplateName);


                        if (mxlt.LootTemplateName.ToLower() == mob.Name.ToLower())
                            isDefaultLootTemplateRefreshed = true;
                    }
                }
            }

            // now force a refresh of the mobs default loot template
            if (isDefaultLootTemplateRefreshed == false)
                RefreshLootTemplate(mob.Name);
        }

        protected void RefreshLootTemplate(string templateName)
        {
            var lootTemplates = DOLDB<DropTemplateXItemTemplate>.SelectObjects(DB.Column(nameof(DropTemplateXItemTemplate.TemplateName)).IsEqualTo(templateName));

            if (lootTemplates != null)
            {
                lock (m_lootTemplates)
                {
                    m_lootTemplates.Remove(templateName.ToLower());

                    var lootList = new Dictionary<string, DropTemplateXItemTemplate>();
                    foreach (DropTemplateXItemTemplate lt in lootTemplates)
                    {
                        if (lootList.ContainsKey(lt.ItemTemplateID.ToLower()) == false)
                        {
                            lootList.Add(lt.ItemTemplateID.ToLower(), lt);
                        }
                    }

                    m_lootTemplates.Add(templateName.ToLower(), lootList);
                }
            }
        }

        private static bool IsWithinHourRange(int hourMin, int hourMax, GamePlayer player)
        {
            uint currentTick = WorldMgr.GetCurrentGameTime(player);
            uint minTick = ((uint)hourMin) * 60 * 60 * 1000;
            uint maxTick = ((uint)hourMax) * 60 * 60 * 1000;

            if (maxTick < minTick)
                return currentTick >= minTick || currentTick <= maxTick;
            else
                return currentTick >= minTick && currentTick <= maxTick;
        }

        private static bool IsQuestStepCompleted(GamePlayer player, int questID, int questStepID)
        {
            var quest = DataQuestJsonMgr.GetQuest((ushort)questID);
            if (quest == null) return false;

            if (questID == 0 || questStepID == 0)
            {
                return true;
            }

            if (questStepID > 0)
            {
                var playerQuest = player.IsDoingQuest(quest);
                return playerQuest != null && playerQuest.GoalStates.Any(g => g.GoalId == questStepID && g.IsActive);
            }
            else
            {
                return player.HasFinishedQuest(quest) == 1;
            }
        }

        public override LootList GenerateLoot(GameObject mob, GameObject killer)
        {
            LootList loot = base.GenerateLoot(mob, killer);

            try
            {
                if ((killer as GameLiving)?.GetController() is not GamePlayer player)
                    return loot;

                // allow the leader to decide the loot realm
                if (player.Group != null)
                    player = player.Group.Leader;

                List<MobDropTemplate> killedMobXLootTemplates;
                // MobDropTemplate contains a loot template name and the max number of drops allowed for that template.
                // We don't need an entry in MobDropTemplate in order to drop loot, only to control the max number of drops.

                // DropTemplateXItemTemplate contains a template name and an itemtemplateid (id_nb).
                // TemplateName usually equals Mob name, so if you want to know what drops for a mob:
                // select * from DropTemplateXItemTemplate where templatename = 'mob name';
                // It is possible to have TemplateName != MobName but this works only if there is an entry in MobDropTemplate for the MobName.
                if (!m_mobXLootTemplates.TryGetValue(mob.Name.ToLower(), out killedMobXLootTemplates))
                {
                    Dictionary<string, DropTemplateXItemTemplate> lootTemplatesToDrop;
                    // We can use DropTemplateXItemTemplate.Count to determine how many of a item can drop
                    if (m_lootTemplates.TryGetValue(mob.Name.ToLower(), out lootTemplatesToDrop))
                    {
                        foreach (DropTemplateXItemTemplate lootTemplate in lootTemplatesToDrop.Values)
                        {
                            ItemTemplate drop = GameServer.Database.FindObjectByKey<ItemTemplate>(lootTemplate.ItemTemplateID);

                            if (drop.Realm == (int)player.Realm || drop.Realm == 0 || player.CanUseCrossRealmItems)
                            {
                                int lootChanceModifier = player.LootChance;
                                int finalChance = Math.Min(100, lootTemplate.Chance + lootChanceModifier);

                                if (!string.IsNullOrEmpty(lootTemplate.ActiveEventId))
                                {
                                    var activeEvent = GameEventManager.Instance.GetEventByID(lootTemplate.ActiveEventId);
                                    if (activeEvent == null || !activeEvent.StartedTime.HasValue || (activeEvent.EndTime.HasValue && DateTimeOffset.UtcNow > activeEvent.EndTime.Value))
                                    {
                                        continue;
                                    }
                                }

                                if (player.Level >= lootTemplate.MinLevel && player.Level <= lootTemplate.MaxLevel &&
                                    IsWithinHourRange(lootTemplate.HourMin, lootTemplate.HourMax, player) &&
                                    (lootTemplate.QuestID == 0 || IsQuestStepCompleted(player, lootTemplate.QuestID, lootTemplate.QuestStepID)) && (!lootTemplate.IsRenaissance || player.IsRenaissance))
                                {
                                    if (finalChance == 100)
                                        loot.AddFixed(drop, lootTemplate.Count);
                                    else
                                        loot.AddRandom(finalChance, drop, lootTemplate.Count);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // MobDropTemplate exists and tells us the max number of items that can drop.
                    // Because we are restricting the max number of items to drop we need to traverse the list
                    // and add every 100% chance items to the loots Fixed list and add the rest to the Random list
                    // due to the fact that 100% items always drop regardless of the drop limit
                    List<DropTemplateXItemTemplate> lootTemplatesToDrop = new List<DropTemplateXItemTemplate>();
                    bool hasDefaultDrops = false;
                    foreach (MobDropTemplate mobXLootTemplate in killedMobXLootTemplates)
                    {
                        if (string.Equals(mobXLootTemplate.LootTemplateName, mob.Name, StringComparison.InvariantCultureIgnoreCase))
                            hasDefaultDrops = true;
                        loot = GenerateLootFromMobXLootTemplates(mobXLootTemplate.LootTemplateName.ToLower(), lootTemplatesToDrop, loot, player);
                        loot.DropCount = Math.Max(mobXLootTemplate.DropCount, loot.DropCount);
                    }
                    if (!hasDefaultDrops)
                    {
                        GenerateLootFromMobXLootTemplates(mob.Name, lootTemplatesToDrop, loot, player);
                    }
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Error in LootGeneratorTemplate for mob {0}.  Exception: {1}", mob.Name, ex.Message);
            }

            return loot;
        }

        /// <summary>
        /// Add all loot templates specified in MobDropTemplate for an entry in DropTemplateXItemTemplate
        /// If the item has a 100% drop chance add it as a fixed drop to the loot list.  
        /// </summary>
        /// <param name="mobXLootTemplate">Entry in MobDropTemplate.</param>
        /// <param name="lootTemplates">List of all itemtemplates this mob can drop and the chance to drop</param>
        /// <param name="lootList">List to hold loot.</param>
        /// <param name="player">Player used to determine realm</param>
        /// <returns>lootList (for readability)</returns>
        private static LootList GenerateLootFromMobXLootTemplates(string lootTemplateName, List<DropTemplateXItemTemplate> lootTemplates, LootList lootList, GamePlayer player)
        {
            if (string.IsNullOrEmpty(lootTemplateName) || lootTemplates == null || player == null)
                return lootList;

            Dictionary<string, DropTemplateXItemTemplate> templateList = null;
            if (m_lootTemplates.TryGetValue(lootTemplateName.ToLower(), out templateList))
            {
                foreach (DropTemplateXItemTemplate lootTemplate in templateList.Values)
                {
                    ItemTemplate drop = GameServer.Database.FindObjectByKey<ItemTemplate>(lootTemplate.ItemTemplateID);

                    if (drop.Realm == (int)player.Realm || drop.Realm == 0 || player.CanUseCrossRealmItems)
                    {
                        int lootChanceModifier = player.LootChance;
                        int finalChance = Math.Min(100, lootTemplate.Chance + lootChanceModifier);

                        if (!string.IsNullOrEmpty(lootTemplate.ActiveEventId))
                        {
                            var activeEvent = GameEventManager.Instance.GetEventByID(lootTemplate.ActiveEventId);
                            if (activeEvent == null || !activeEvent.StartedTime.HasValue || (activeEvent.EndTime.HasValue && DateTimeOffset.UtcNow > activeEvent.EndTime.Value))
                            {
                                continue; // Skip loot if the event is not active
                            }
                        }

                        if (player.Level >= lootTemplate.MinLevel && player.Level <= lootTemplate.MaxLevel &&
                            IsWithinHourRange(lootTemplate.HourMin, lootTemplate.HourMax, player) &&
                            (lootTemplate.QuestID == 0 || IsQuestStepCompleted(player, lootTemplate.QuestID, lootTemplate.QuestStepID)) && (!lootTemplate.IsRenaissance || player.IsRenaissance))
                        {
                            if (finalChance == 100)
                                lootList.AddFixed(drop, lootTemplate.Count);
                            else
                                lootList.AddRandom(finalChance, drop, lootTemplate.Count);
                        }
                    }
                }
            }

            return lootList;
        }

    }
}
