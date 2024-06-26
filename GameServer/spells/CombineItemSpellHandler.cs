﻿using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.GS.Spells;
using DOL.Language;
using DOLDatabase.Tables;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DOL.spells
{
    [SpellHandler("CombineItem")]
    public class CombineItemSpellHandler
        : SpellHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly int[] modelForge = { 478, 1495 };
        private static readonly int[] modelTannery = { 479 };
        private static readonly int[] modelWeaver = { 480 };
        private static readonly int[] modelcarpentryWorkshop = { 481 };
        private static readonly int[] modelCauldron = { 632, 2607, 3475, 4203 };
        private static readonly int[] modelAlchemyTable = { 820, 1494 };
        private static readonly int[] modelFire = { 2656, 3460, 3470, 3549 };
        private static readonly int[] modelChest = { 1596, 4182, 4183, 4184 };

        private Combinable match;
        private WorldInventoryItem combined;
        // Transfor the list to a dictionary to count the number of items to remove
        private Dictionary<InventoryItem, int> removeItems;
        InventoryItem useItem;


        public CombineItemSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine)
            : base(caster, spell, spellLine)
        {
        }

        /// <summary>
        /// Check whether it's actually possible to do the combine.
        /// </summary>
        /// <param name="selectedTarget"></param>
        /// <returns></returns>
        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (!base.CheckBeginCast(selectedTarget))
            {
                return false;
            }

            if (!(Caster is GamePlayer player))
            {
                return false;
            }

            useItem = player.UseItem;
            if (useItem == null)
            {
                return false;
            }

            var neededItems = this.GetCombinableItems(useItem.Id_nb);

            if (neededItems == null || !neededItems.Any())
            {
                return false;
            }

            removeItems = new Dictionary<InventoryItem, int>();

            var backpack = player.Inventory.GetItemRange(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
            match = null;

            foreach (var combinable in neededItems)
            {
                List<string> ids = new List<string>();
                Dictionary<string, int> countIterator = new Dictionary<string, int>();
                foreach (InventoryItem item in backpack)
                {
                    // check if the number of item is greater or equal than needed
                    if (item != null && combinable.Items.ContainsKey(item.Id_nb) && (item.Count >= combinable.Items[item.Id_nb] || (countIterator.ContainsKey(item.Id_nb) && (countIterator[item.Id_nb] + item.Count) >= combinable.Items[item.Id_nb])))
                    {
                        if (!ids.Contains(item.Id_nb))
                        {
                            ids.Add(item.Id_nb);
                            // If items have already added in the remove list, substract the total of removed items, else add the number of items to delete
                            if (countIterator.ContainsKey(item.Id_nb))
                                removeItems.Add(item, combinable.Items[item.Id_nb] - countIterator[item.Id_nb]);
                            else
                                removeItems.Add(item, combinable.Items[item.Id_nb]);
                        }
                    }
                    else if (item != null && combinable.Items.ContainsKey(item.Id_nb))
                    {
                        if (countIterator.ContainsKey(item.Id_nb))
                            countIterator[item.Id_nb] += item.Count;
                        else
                            countIterator.Add(item.Id_nb, item.Count);
                        // fix the issue when items take several slots
                        removeItems.Add(item, item.Count);
                    }
                }

                if (ids.Count == combinable.Items.Count())
                {
                    match = combinable;
                    break;
                }

                removeItems.Clear();
            }

            if (match == null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Spell.CombineItemSpellHandler.NoCombinePossible"), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (match.CraftingSkill != eCraftingSkill.NoCrafting && match.CraftValue > 0)
            {
                if (!player.CraftingSkills.ContainsKey(match.CraftingSkill))
                {
                    log.Warn($"Combine Item: Crafting skill {match.CraftingSkill.ToString()} not found in Player {player.InternalID} Crafting Skill");
                    return false;
                }
                else if (player.CraftingSkills[match.CraftingSkill] < match.CraftValue && !match.ApplyRewardCraftingSkillsSystem)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Spell.CombineItemSpellHandler.CraftingSkillTooLow", match.CraftingSkill.ToString(), match.CraftValue), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            // should not pass here but keep it in case where I'm wrong
            if (!removeItems.ContainsKey(useItem))
                removeItems.Add(useItem, match.Items[useItem.Id_nb]);

            combined = CreateCombinedItem(match, player);

            if (combined == null)
            {
                log.Warn($"Missing item in ItemTemplate table '{match.TemplateId}' for CombineItem spell");
                return false;
            }
            combined.Item.Count = match.ItemsCount;

            // check if player is in the area if it needed
            if (match.AreaId != null)
            {
                bool checkAreaId = false;
                foreach (IArea iarea in player.CurrentAreas)
                    if (iarea is AbstractArea area && area.DbArea != null && area.DbArea.ObjectId == match.AreaId)
                        checkAreaId = true;
                if (!checkAreaId)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Spell.CombineItemSpellHandler.CantCombineHere"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            if (match.CombinexObjectModel != null && !CheckForTools(player, match.CombinexObjectModel.Split(new char[] { '|' }))
                && (string.IsNullOrEmpty(match.ToolKit) || !CheckToolKit(player, match.ToolKit)))
                return false;
            if (!CheckToolKit(player, match.ToolKit))
                return false;

            int adjustedDuration = AdjustDurationForCraftingBonuses(player, match.Duration);
            if (adjustedDuration > 0)
            {
                player.Out.SendTimerWindow("Combinaison en cours: " + combined.Item.Name, adjustedDuration);
                GameEventMgr.RemoveHandler(Caster, GameLivingEvent.Moving, new DOLEventHandler(EventManager));
                GameEventMgr.RemoveHandler(Caster, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventManager));
                GameEventMgr.AddHandler(Caster, GameLivingEvent.Moving, new DOLEventHandler(EventManager));
                GameEventMgr.AddHandler(Caster, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventManager));
            }

            return true;
        }

        private bool CheckToolKit(GamePlayer player, string toolKit)
        {
            if (string.IsNullOrEmpty(toolKit))
                return true;
            List<InventoryItem> items = player.Inventory.GetItemRange(eInventorySlot.MinEquipable, eInventorySlot.LastBackpack).ToList();
            foreach (InventoryItem item in items)
            {
                if (item.Id_nb == toolKit)
                    return true;
            }
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Spell.CombineItemSpellHandler.Toolkit", toolKit), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return false;
        }

        public override bool CastSpell(GameLiving targetObject)
        {
            if (match.Duration > 0)
            {
                StartCastTimer(m_spellTarget);

                if ((Caster is GamePlayer && (Caster as GamePlayer).IsStrafing) || Caster.IsMoving)
                    CasterMoves();
            }
            else
            {
                SendCastAnimation(0);

                FinishSpellCast(m_spellTarget);
            }
            if (!IsCasting)
                OnAfterSpellCastSequence();
            return true;
        }

        public override int CalculateCastingTime()
        {
            return AdjustDurationForCraftingBonuses((GamePlayer)Caster, match.Duration) * 1000;
        }

        private void EventManager(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.RemoveHandler(Caster, GameLivingEvent.Moving, new DOLEventHandler(EventManager));
            GameEventMgr.RemoveHandler(Caster, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventManager));
            if (e == GameLivingEvent.Moving)
                MessageToCaster(LanguageMgr.GetTranslation(((GamePlayer)Caster).Client.Account.Language, "AssassinateAbility.Moving"), eChatType.CT_Important);
            if (e == GameLivingEvent.AttackedByEnemy)
                MessageToCaster(LanguageMgr.GetTranslation(((GamePlayer)Caster).Client.Account.Language, "AssassinateAbility.Attacked"), eChatType.CT_Important);
            InterruptCasting();
        }

        public override void InterruptCasting()
        {
            base.InterruptCasting();
            if (match.Duration > 0)
                ((GamePlayer)Caster).Out.SendCloseTimerWindow();
        }

        /// <summary>
        /// Check if the player is near the needed tools (forge, lathe, etc)
        /// </summary>
        /// <param name="player">the crafting player</param>
        /// <param name="models">model for tools needed</param>
        /// <returns>true if required tools are found</returns>
        private bool CheckForTools(GamePlayer player, IList<string> models)
        {
            int modelCase = 0;
            bool differentModels = false;

            string toolName = LanguageMgr.GetTranslation(player.Client, "ToolName.Object");
            string endString = LanguageMgr.GetTranslation(player.Client, "Spell.CombineItemSpellHandler.PerformCombination");

            List<int> mergeModelList = new List<int>();

            foreach (var model in models)
            {
                var modelParts = model.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in modelParts)
                {
                    if (int.TryParse(part, out int modelNumber))
                    {
                        mergeModelList.Add(modelNumber);
                    }
                }
            }

            if (mergeModelList.Intersect(modelForge).Count() > 0)
            {
                modelCase = 1;
                mergeModelList = new List<int>(mergeModelList.Union(modelForge));
            }
            if (mergeModelList.Intersect(modelTannery).Count() > 0)
            {
                if (modelCase > 0)
                    differentModels = true;
                modelCase = 2;
                mergeModelList = new List<int>(mergeModelList.Union(modelTannery));
            }
            if (mergeModelList.Intersect(modelWeaver).Count() > 0)
            {
                if (modelCase > 0)
                    differentModels = true;
                modelCase = 3;
                mergeModelList = new List<int>(mergeModelList.Union(modelWeaver));
            }
            if (mergeModelList.Intersect(modelcarpentryWorkshop).Count() > 0)
            {
                if (modelCase > 0)
                    differentModels = true;
                modelCase = 4;
                mergeModelList = new List<int>(mergeModelList.Union(modelcarpentryWorkshop));
            }
            if (mergeModelList.Intersect(modelCauldron).Count() > 0)
            {
                if (modelCase > 0)
                    differentModels = true;
                modelCase = 5;
                mergeModelList = new List<int>(mergeModelList.Union(modelCauldron));
            }
            if (mergeModelList.Intersect(modelAlchemyTable).Count() > 0)
            {
                if (modelCase > 0)
                    differentModels = true;
                modelCase = 6;
                mergeModelList = new List<int>(mergeModelList.Union(modelAlchemyTable));
            }
            if (mergeModelList.Intersect(modelFire).Count() > 0)
            {
                if (modelCase > 0)
                    differentModels = true;
                modelCase = 7;
                mergeModelList = new List<int>(mergeModelList.Union(modelFire));
            }
            if (mergeModelList.Intersect(modelChest).Count() > 0)
            {
                if (modelCase > 0)
                    modelCase = 0;
                else
                    modelCase = 8;
                mergeModelList = new List<int>(mergeModelList.Union(modelChest));
            }

            IEnumerable<GameObject> itemsInRadius = player.GetItemsInRadius(350).Cast<GameObject>();
            IEnumerable<GameObject> ncpInRadius = player.GetNPCsInRadius(350).Cast<GameObject>();
            foreach (GameObject item in new List<GameObject>(itemsInRadius.Concat(ncpInRadius)))
            {
                if (mergeModelList.Contains(item.Model))
                {
                    return true;
                }
            }
            if (differentModels)
            {
                modelCase = 0;
            }
            switch (modelCase)
            {
                case 1:
                    toolName = LanguageMgr.GetTranslation(player.Client, "ToolName.Forge");
                    break;
                case 2:
                    toolName = LanguageMgr.GetTranslation(player.Client, "ToolName.Tannery");
                    break;
                case 3:
                    toolName = LanguageMgr.GetTranslation(player.Client, "ToolName.Weaver");
                    break;
                case 4:
                    toolName = LanguageMgr.GetTranslation(player.Client, "ToolName.CarpentryWorkshop");
                    break;
                case 5:
                    toolName = LanguageMgr.GetTranslation(player.Client, "ToolName.Cauldron");
                    break;
                case 6:
                    toolName = LanguageMgr.GetTranslation(player.Client, "ToolName.AlchemyTable");
                    break;
                case 7:
                    toolName = LanguageMgr.GetTranslation(player.Client, "ToolName.Fire");
                    break;
                case 8:
                    toolName = LanguageMgr.GetTranslation(player.Client, "ToolName.Chest");
                    break;
                default:
                    toolName = LanguageMgr.GetTranslation(player.Client, "ToolName.Object");
                    endString = LanguageMgr.GetTranslation(player.Client, "Spell.CombineItemSpellHandler.PerformCombination");
                    break;
            }

            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Spell.CombineItemSpellHandler.NeedToApproach", toolName, endString), eChatType.CT_System, eChatLoc.CL_SystemWindow);

#if (RELEASE)
            if (player.Client.Account.PrivLevel > 1)
            {
                return true;
            }
#endif

            return false;
        }

        private int GetQuality(GamePlayer player, int craftValue)
        {
            int con = GetItemCon(player.CraftingSkills[match.CraftingSkill], craftValue);
            int[] chancePart;
            int sum = 0;
            int baseQuality;
            int qualityRange;

            switch (con)
            {
                case 3:
                case 4:
                    baseQuality = 78;
                    chancePart = new int[] { 2, 6, 14, 32, 26, 13, 7 };
                    qualityRange = 7;
                    break;
                case 2:
                    baseQuality = 82;
                    chancePart = new int[] { 1, 6, 12, 28, 30, 15, 8 };
                    qualityRange = 7;
                    break;
                case 1:
                    baseQuality = 86;
                    chancePart = new int[] { 1, 5, 14, 28, 30, 15, 7 };
                    qualityRange = 7;
                    break;
                case 0:
                    baseQuality = 92;
                    chancePart = new int[] { 6, 10, 13, 19, 20, 16, 15, 1 };
                    qualityRange = 8;
                    break;
                case -1:
                    baseQuality = 94;
                    chancePart = new int[] { 17, 17, 18, 20, 18, 10 };
                    qualityRange = 6;
                    break;
                case -2:
                    baseQuality = 96;
                    chancePart = new int[] { 18, 21, 25, 23, 13 };
                    qualityRange = 5;
                    break;
                case -3:
                case -4:
                default:
                    baseQuality = 97;
                    chancePart = new int[] { 24, 27, 31, 18 };
                    qualityRange = 4;
                    break;
            }

            sum = chancePart.Sum();
            int rand = Util.Random(sum);

            for (int i = 0; i < qualityRange; i++)
            {
                if (rand < chancePart[i])
                    return baseQuality + i;
                rand -= chancePart[i];
            }

            return baseQuality;
        }

        /// <summary>
        /// Do the combine.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effectiveness"></param>
        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            GamePlayer player = Caster as GamePlayer;
            GameEventMgr.RemoveHandler(Caster, GameLivingEvent.Moving, new DOLEventHandler(EventManager));
            GameEventMgr.RemoveHandler(Caster, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventManager));
            player.Out.SendCloseTimerWindow();

            if (!string.IsNullOrEmpty(match.CombinationId))
            {
                CharacterXCombineItem characterXCombineItem = (CharacterXCombineItem)GameServer.Database.SelectObjects<CharacterXCombineItem>(
                    DB.Column("CombinationId").IsEqualTo(player.InternalID)
                    .And(DB.Column("Character_ID").IsEqualTo(match.CombinationId)));
                if (characterXCombineItem == null)
                {
                    characterXCombineItem = new CharacterXCombineItem(player.InternalID, match.CombinationId);
                    GameServer.Database.AddObject(characterXCombineItem);
                }
            }

            // Check if player fail to combine
            if (Util.Chance(CalculateChanceFailCombine(player)))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Spell.CombineItemSpellHandler.YouFailedCombine"), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
                player.Out.SendPlaySound(eSoundType.Craft, 0x02);
                if (match.PunishSpell != 0)
                {
                    var punishSpell = GameServer.Database.SelectObject<DBSpell>(DB.Column("SpellID").IsEqualTo(match.PunishSpell));

                    if (punishSpell != null)
                    {
                        foreach (GamePlayer pl in player.GetPlayersInRadius(5000))
                        {
                            pl.Out.SendSpellEffectAnimation(player, player, (ushort)match.PunishSpell, 0, false, 5);
                        }
                        player.Out.SendSpellEffectAnimation(player, player, (ushort)match.PunishSpell, 0, false, 5);
                        player.TakeDamage(player, eDamageType.Energy, (int)punishSpell.Damage, 0);
                    }
                }
                RemoveItems(player, removeItems);
                return;
            }

            InventoryItem newItem = combined.Item;
            if (match.IsUnique)
            {
                ItemUnique unique = new ItemUnique(combined.Item.Template);
                unique.IsTradable = true;
                GameServer.Database.AddObject(unique);
                newItem = GameInventoryItem.Create(unique);

                if (match.ApplyRewardCraftingSkillsSystem)
                {
                    // Calculate quality using the new method
                    newItem.Quality = GetQuality(player, match.CraftValue);
                }
                else
                {
                    // Calculate quality using the previous method
                    int randomQuality = Util.Random(99);
                    if (randomQuality < 16)
                    {
                        newItem.Quality = 95;
                    }
                    else if (randomQuality < 36)
                    {
                        newItem.Quality = 96;
                    }
                    else if (randomQuality < 56)
                    {
                        newItem.Quality = 97;
                    }
                    else if (randomQuality < 76)
                    {
                        newItem.Quality = 98;
                    }
                    else if (randomQuality < 96)
                    {
                        newItem.Quality = 99;
                    }
                    else
                    {
                        newItem.Quality = 100;
                    }
                }

                newItem.IsCrafted = true;
                newItem.Creator = player.Name;
                newItem.Count = match.ItemsCount;
            }

            player.Out.SendSpellEffectAnimation(player, player, (ushort)match.SpellEfect, 0, false, 1);

            int con = 0;
            if (match.CraftingSkill != eCraftingSkill.NoCrafting)
            {
                con = GetItemCon(player.CraftingSkills[match.CraftingSkill], match.CraftValue);
            }

            if (newItem.Quality == 100)
            {
                player.Out.SendPlaySound(eSoundType.Craft, 0x04);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "AbstractCraftingSkill.BuildCraftedItem.Masterpiece", newItem.Name, useItem.Name, match.Items.Count() - 1), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
                if (match.ApplyRewardCraftingSkillsSystem)
                {
                    if (con > -3)
                    {
                        TaskManager.UpdateTaskProgress(player, "MasterpieceCrafted", 1);
                    }
                }
                else
                {
                    if (con > -3)
                    {
                        TaskManager.UpdateTaskProgress(player, "MasterpieceCrafted", 1);
                    }
                }
            }
            else
            {
                player.Out.SendPlaySound(eSoundType.Craft, 0x03);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Spell.CombineItemSpellHandler.YouCreatedItem", newItem.Name, useItem.Name, match.Items.Count() - 1), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                if (match.ApplyRewardCraftingSkillsSystem)
                {
                    if (con > -2)
                    {
                        TaskManager.UpdateTaskProgress(player, "SuccessfulItemCombinations", 1);
                    }
                }
                else
                {
                    if (con > -2)
                    {
                        TaskManager.UpdateTaskProgress(player, "SuccessfulItemCombinations", 1);
                    }
                }
            }
            RemoveItems(player, removeItems);

            if (match.RewardCraftingSkills > 0)
            {
                int gain = match.RewardCraftingSkills;
                if (match.ApplyRewardCraftingSkillsSystem && player.CraftingSkills.ContainsKey(match.CraftingSkill))
                {
                    gain = CalculateRewardCraftingSkill(player, match.CraftingSkill, match.CraftValue, match.RewardCraftingSkills);
                }
                player.GainCraftingSkill(match.CraftingSkill, gain);
                player.Out.SendUpdateCraftingSkills();
            }

            if (!string.IsNullOrEmpty(match.ToolKit) && match.ToolLoseDur > 0)
            {

                InventoryItem toolkit = player.Inventory.GetItemRange(eInventorySlot.MinEquipable, eInventorySlot.LastBackpack).Where(item => item.Id_nb == match.ToolKit).FirstOrDefault();
                if (toolkit != null)
                {
                    toolkit.Durability -= match.ToolLoseDur;
                    if (toolkit.Durability <= 0)
                        player.Inventory.RemoveItem(toolkit);
                    else
                        player.Out.SendInventoryItemsUpdate(new InventoryItem[] { toolkit });
                }
            }

            if (!player.ReceiveItem(player, newItem))
            {
                player.CreateItemOnTheGround(newItem);
                player.Out.SendDialogBox(eDialogCode.SimpleWarning, 0, 0, 0, 0, eDialogType.Ok, true, LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.BuildCraftedItem.BackpackFull", newItem.Name));
            }
        }

        private int CalculateRewardCraftingSkill(GamePlayer player, eCraftingSkill craftingSkill, int craftValue, int baseReward)
        {
            int con = GetItemCon(player.CraftingSkills[craftingSkill], craftValue);
            int reward;

            switch (con)
            {
                case -3:
                    reward = (int)Math.Round(baseReward * 0.4);
                    break;
                case -2:
                    reward = (int)Math.Round(baseReward * 0.7);
                    break;
                case -1:
                    reward = (int)Math.Round(baseReward * 0.85);
                    break;
                case 0:
                    reward = baseReward;
                    break;
                case 1:
                    reward = (int)Math.Round(baseReward * 1.1);
                    break;
                case 2:
                    reward = (int)Math.Round(baseReward * 1.2);
                    break;
                case 3:
                    reward = (int)Math.Round(baseReward * 1.4);
                    break;
                case 4:
                case -4:
                default:
                    reward = 0;
                    break;
            }

            return reward;
        }

        private int CalculateChanceFailCombine(GamePlayer player)
        {
            if (!match.ApplyRewardCraftingSkillsSystem)
                return match.ChanceFailCombine;

            int con = GetItemCon(player.CraftingSkills[match.CraftingSkill], match.CraftValue);
            int failChance = match.ChanceFailCombine;

            switch (con)
            {
                case -4:
                    failChance = Math.Min(100, failChance - (int)Math.Round(failChance * 0.9));
                    break;
                case -3:
                    failChance = Math.Min(100, failChance - (int)Math.Round(failChance * 0.75));
                    break;
                case -2:
                    failChance = Math.Min(100, failChance - (int)Math.Round(failChance * 0.4));
                    break;
                case -1:
                    failChance = Math.Min(100, failChance - (int)Math.Round(failChance * 0.16));
                    break;
                case 0:
                    break;
                case 1:
                    failChance = Math.Max(0, failChance + (int)Math.Round(failChance * 0.2));
                    break;
                case 2:
                    failChance = Math.Max(0, failChance + (int)Math.Round(failChance * 0.4));
                    break;
                case 3:
                    failChance = Math.Max(0, failChance + (int)Math.Round(failChance * 0.88));
                    break;
                case 4:
                default:
                    failChance = 100;
                    break;
            }

            return failChance;
        }

        private int GetItemCon(int crafterSkill, int itemCraftingLevel)
        {
            int diff = itemCraftingLevel - crafterSkill;
            if (diff <= -70)
                return -4;
            else if (diff <= -50)
                return -3;
            else if (diff <= -31)
                return -2;
            else if (diff <= -11)
                return -1;
            else if (diff <= 0)
                return 0;
            else if (diff <= 19)
                return 1;
            else if (diff <= 49)
                return 2;
            else if (diff <= 74)
                return 3;
            else
                return 4;
        }

        private IEnumerable<Combinable> GetCombinableItems(string usedItemId)
        {
            var cbitems = GameServer.Database.SelectAllObjects<CombineItemDb>();

            if (cbitems == null || !cbitems.Any())
            {
                return null;
            }

            var neededItems = cbitems.Select(
                c =>
                {
                    // Add the count of used items for a specific template to combine it with others templates
                    string[] itemsNameCount = c.ItemsIds.Split(new char[] { ';' });
                    Dictionary<string, int> items = new Dictionary<string, int>();
                    foreach (string item in itemsNameCount)
                    {
                        string[] itemNameCount = item.Split(new char[] { '|' });
                        int count;
                        if (int.TryParse(itemNameCount[1], out count))
                            items.Add(itemNameCount[0], count);
                    }
                    return new Combinable()
                    {
                        Items = items,
                        TemplateId = c.ItemTemplateId.Split(new char[] { '|' })[0],
                        ItemsCount = int.Parse(c.ItemTemplateId.Split(new char[] { '|' })[1]),
                        SpellEfect = c.SpellEffect,
                        CraftingSkill = (eCraftingSkill)c.CraftingSkill,
                        CraftValue = c.CraftingValue,
                        RewardCraftingSkills = c.RewardCraftingSkills,
                        AreaId = c.AreaId,
                        ChanceFailCombine = c.ChanceFailCombine,
                        PunishSpell = c.PunishSpell,
                        CombinexObjectModel = c.CombinexObjectModel,
                        Duration = c.Duration,
                        IsUnique = c.IsUnique,
                        ApplyRewardCraftingSkillsSystem = c.ApplyRewardCraftingSkillsSystem,
                        ToolKit = c.ToolKit,
                        ToolLoseDur = c.ToolLoseDur,
                        CombinationId = c.CombinationId,
                        AllowVersion = c.AllowVersion
                    };
                });

            return neededItems.Where(i => i.Items.ContainsKey(usedItemId));
        }

        private Dictionary<eCraftingSkill, int> BuildRewardSkills(string rawSkills)
        {
            var skills = new Dictionary<eCraftingSkill, int>();

            if (rawSkills == null)
            {
                return skills;
            }

            var skillRaws = rawSkills.Split('|');

            foreach (var skillRaw in skillRaws)
            {
                var values = skillRaw.Split(';');

                if (values.Length == 2)
                {
                    if (Enum.TryParse(values[0], out eCraftingSkill skill) && int.TryParse(values[1], out int val))
                    {
                        if (skill != eCraftingSkill.NoCrafting)
                        {
                            if (!skills.ContainsKey(skill))
                            {
                                skills.Add(skill, val);
                            }
                            else
                            {
                                skills[skill] += val;
                            }
                        }
                    }
                }
            }

            return skills;
        }

        private int AdjustDurationForCraftingBonuses(GamePlayer player, int baseDuration)
        {
            double speedMultiplier = 1.0;

            speedMultiplier *= (1.0 + player.BuffBonusCategory4[eProperty.CraftingSpeed] * 0.01);
            speedMultiplier *= (1.0 + player.ItemBonus[(int)eProperty.CraftingSpeed] * 0.01);

            if (player.Guild != null && player.Guild.BonusType == Guild.eBonusType.CraftingHaste)
            {
                double guildCraftBonus = Properties.GUILD_BUFF_CRAFTING;
                int guildLevel = (int)player.Guild.GuildLevel;

                if (guildLevel >= 8 && guildLevel <= 15)
                {
                    guildCraftBonus *= 1.5;
                }
                else if (guildLevel > 15)
                {
                    guildCraftBonus *= 2.0;
                }

                speedMultiplier *= (1.0 + guildCraftBonus * 0.01);
            }

            return (int)(baseDuration / speedMultiplier);
        }

        /// <summary>
        /// Remove the needed items to combine
        /// </summary>
        /// <param name="player"></param>
        /// <param name="removeItems"></param>
        private void RemoveItems(GamePlayer player, Dictionary<InventoryItem, int> removeItems)
        {
            foreach (InventoryItem item in removeItems.Keys)
            {
                if (item.OwnerID == null)
                    item.OwnerID = player.InternalID;

                // Replace remove item by RemoveCountFromStack
                player.Inventory.RemoveCountFromStack(item, removeItems[item]);
            }
        }

        private WorldInventoryItem CreateCombinedItem(Combinable combinable, GamePlayer player)
        {
            if (combinable.AllowVersion)
            {
                string baseTemplateId = combinable.TemplateId;
                string classSpecificTemplateId = GetClassSpecificTemplateId(baseTemplateId, (eCharacterClass)player.CharacterClass.ID);

                var combinedItem = WorldInventoryItem.CreateFromTemplate(classSpecificTemplateId);
                if (combinedItem == null)
                {
                    log.Warn($"Missing item in ItemTemplate table '{classSpecificTemplateId}' for CombineItem spell");
                    return null;
                }

                return combinedItem;
            }
            else
            {
                return WorldInventoryItem.CreateFromTemplate(combinable.TemplateId);
            }
        }

        private string GetClassSpecificTemplateId(string baseTemplateId, eCharacterClass characterClass)
        {
            string prefix = GetArmorPrefix(characterClass);
            return $"{baseTemplateId}_{prefix}";
        }

        private string GetArmorPrefix(eCharacterClass characterClass)
        {
            switch (characterClass)
            {
                case eCharacterClass.Paladin:
                case eCharacterClass.Armsman:
                    return "plate";
                case eCharacterClass.Hero:
                case eCharacterClass.Champion:
                case eCharacterClass.Warden:
                case eCharacterClass.Druid:
                    return "scale";
                case eCharacterClass.Mercenary:
                case eCharacterClass.Cleric:
                case eCharacterClass.Reaver:
                case eCharacterClass.Thane:
                case eCharacterClass.Warrior:
                case eCharacterClass.Valkyrie:
                case eCharacterClass.Healer:
                case eCharacterClass.Shaman:
                case eCharacterClass.Skald:
                    return "chain";
                case eCharacterClass.Blademaster:
                case eCharacterClass.Nightshade:
                case eCharacterClass.Ranger:
                case eCharacterClass.Bard:
                case eCharacterClass.Guardian:
                    return "reinforced";
                case eCharacterClass.Infiltrator:
                case eCharacterClass.Scout:
                case eCharacterClass.Minstrel:
                case eCharacterClass.Naturalist:
                case eCharacterClass.Berserker:
                case eCharacterClass.Hunter:
                case eCharacterClass.Shadowblade:
                case eCharacterClass.Savage:
                case eCharacterClass.Viking:
                case eCharacterClass.Fighter:
                    return "studded";
                case eCharacterClass.Vampiir:
                case eCharacterClass.MaulerAlb:
                case eCharacterClass.MaulerMid:
                case eCharacterClass.MaulerHib:
                case eCharacterClass.Heretic:
                case eCharacterClass.Friar:
                case eCharacterClass.Stalker:
                case eCharacterClass.MidgardRogue:
                case eCharacterClass.AlbionRogue:
                    return "leather";
                case eCharacterClass.Wizard:
                case eCharacterClass.Sorcerer:
                case eCharacterClass.Theurgist:
                case eCharacterClass.Necromancer:
                case eCharacterClass.Cabalist:
                case eCharacterClass.Acolyte:
                case eCharacterClass.Spiritmaster:
                case eCharacterClass.Runemaster:
                case eCharacterClass.Bonedancer:
                case eCharacterClass.Warlock:
                case eCharacterClass.Bainshee:
                case eCharacterClass.Eldritch:
                case eCharacterClass.Enchanter:
                case eCharacterClass.Mentalist:
                case eCharacterClass.Animist:
                case eCharacterClass.Valewalker:
                case eCharacterClass.Mage:
                case eCharacterClass.Elementalist:
                case eCharacterClass.Magician:
                case eCharacterClass.Forester:
                case eCharacterClass.Mystic:
                case eCharacterClass.Seer:
                case eCharacterClass.Disciple:
                    return "cloth";
                default:
                    return "generic";
            }
        }
    }

    public class Combinable
    {
        public Dictionary<string, int> Items { get; set; }
        public string TemplateId { get; set; }
        public int ItemsCount { get; set; }
        public int SpellEfect { get; set; }
        public eCraftingSkill CraftingSkill { get; set; }
        public int CraftValue { get; set; }
        public int RewardCraftingSkills { get; set; }
        public string AreaId { get; set; }
        public int ChanceFailCombine { get; set; }
        public int PunishSpell { get; set; }
        public int Duration { get; set; }
        public string CombinexObjectModel { get; set; }
        public bool IsUnique { get; set; }
        public bool ApplyRewardCraftingSkillsSystem { get; set; }
        public string ToolKit { get; set; }
        public short ToolLoseDur { get; set; }
        public string CombinationId { get; set; }
        public bool AllowVersion { get; set; }
    }
}

