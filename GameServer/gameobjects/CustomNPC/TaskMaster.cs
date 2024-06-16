using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS.Quests;
using DOL.GS.Scripts;
using log4net;
using DOL.GS.PlayerTitles;
using Discord;

namespace DOL.GS
{
    public class TaskMaster : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public TextNPCCondition Condition { get; private set; }

        public override eQuestIndicator GetQuestIndicator(GamePlayer player)
        {
            if (player.Reputation >= 0)
                return eQuestIndicator.Lore;
            else return eQuestIndicator.None;
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player) && player.Reputation < 0)
                return false;

            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.Greetings", player.RaceName), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.BonusInfo"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.Taskline"), eChatType.CT_System, eChatLoc.CL_PopupWindow);

            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;

            GamePlayer player = source as GamePlayer;
            if (player == null)
                return false;

            switch (str.ToLower())
            {
                case "services":
                    {
                        //get list of all itextnpcs from db, that have isintaskmaster==1
                        IList<DBTextNPC> taskGivingNPCs = GameServer.Database.SelectObjects<DBTextNPC>(DB.Column("IsInTaskMaster").IsEqualTo("1"));
                        foreach (var taskNPC in taskGivingNPCs)
                        {
                            Condition = new TextNPCCondition(taskNPC.Condition);
                            if (Condition.CheckAccess(player))
                            {
                                var text = taskNPC.MobName + "\n";
                                if (player.Client.Account.Language == "EN")
                                    text += taskNPC.TaskDescEN;
                                else if (player.Client.Account.Language == "FR")
                                    text += taskNPC.TaskDescFR;
                                //get taskNPC 
                                var mob = DOLDB<Mob>.SelectObject(DB.Column("Mob_ID").IsEqualTo(taskNPC.MobID));
                                var region = WorldMgr.GetRegion(mob.Region);
                                if (region != null)
                                {
                                    var zone = region.GetZone(mob.X, mob.Y);
                                    if (zone != null)
                                    {
                                        player.Out.SendMessage(text + "\n" + zone.Description + "\n", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                                    }
                                }
                            }
                        }
                        break;
                    }
                case "tokens":
                case "jetons":
                    {
                        List<string> tokenMessages = new List<string>
                        {
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.TokenInfo"),
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.PvPTokens"),
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.PvETokens"),
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.CraftTokens"),
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.SpecialTokens"),
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.DemonSlayerTokens"),
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.TraderTokens"),
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.ThiefTokens"),
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.BountyHunterTokens"),
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.WrathTokens"),
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.AdventurerTokens"),
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.TokensToDeliver")
                        };

                        foreach (string message in tokenMessages)
                        {
                            player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        }
                        break;
                    }
            }
            return true;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            var player = source as GamePlayer;
            if (item == null || player == null)
            {
                return base.ReceiveItem(source, item);
            }

            string titleKey = null;

            switch (item.Id_nb)
            {
                case "TaskToken_Thief_lv1":
                    AssignTitle(player, new ThiefTitleLevel1());
                    titleKey = "Titles.Thief.Level1";
                    break;
                case "TaskToken_Thief_lv2":
                    AssignTitle(player, new ThiefTitleLevel2());
                    titleKey = "Titles.Thief.Level2";
                    break;
                case "TaskToken_Thief_lv3":
                    AssignTitle(player, new ThiefTitleLevel3());
                    titleKey = "Titles.Thief.Level3";
                    break;
                case "TaskToken_Thief_lv4":
                    AssignTitle(player, new ThiefTitleLevel4());
                    titleKey = "Titles.Thief.Level4";
                    break;
                case "TaskToken_Thief_lv5":
                    AssignTitle(player, new ThiefTitleLevel5());
                    titleKey = "Titles.Thief.Level5";
                    break;
                case "TaskToken_Trader_lv1":
                    AssignTitle(player, new TraderTitleLevel1());
                    titleKey = "Titles.Trader.Level1";
                    break;
                case "TaskToken_Trader_lv2":
                    AssignTitle(player, new TraderTitleLevel2());
                    titleKey = "Titles.Trader.Level2";
                    break;
                case "TaskToken_Trader_lv3":
                    AssignTitle(player, new TraderTitleLevel3());
                    titleKey = "Titles.Trader.Level3";
                    break;
                case "TaskToken_Trader_lv4":
                    AssignTitle(player, new TraderTitleLevel4());
                    titleKey = "Titles.Trader.Level4";
                    break;
                case "TaskToken_Trader_lv5":
                    AssignTitle(player, new TraderTitleLevel5());
                    titleKey = "Titles.Trader.Level5";
                    break;
                case "TaskToken_Demon_Slayer_lv1":
                    AssignTitle(player, new DemonslayerTitleLevel1());
                    titleKey = "Titles.Demonslayer.Level1";
                    break;
                case "TaskToken_Demonslayer_lv2":
                    AssignTitle(player, new DemonslayerTitleLevel2());
                    titleKey = "Titles.Demonslayer.Level2";
                    break;
                case "TaskToken_Demon_Slayer_lv3":
                    AssignTitle(player, new DemonslayerTitleLevel3());
                    titleKey = "Titles.Demonslayer.Level3";
                    break;
                case "TaskToken_Demon_Slayer_lv4":
                    AssignTitle(player, new DemonslayerTitleLevel4());
                    titleKey = "Titles.Demonslayer.Level4";
                    break;
                case "TaskToken_Demon_Slayer_lv5":
                    AssignTitle(player, new DemonslayerTitleLevel5());
                    titleKey = "Titles.Demonslayer.Level5";
                    break;
                case "TaskToken_Bounty_Hunter_lv1":
                    AssignTitle(player, new BountyhunterTitleLevel1());
                    titleKey = "Titles.Bountyhunter.Level1";
                    break;
                case "TaskToken_Bounty_Hunter_lv2":
                    AssignTitle(player, new BountyhunterTitleLevel2());
                    titleKey = "Titles.Bountyhunter.Level2";
                    break;
                case "TaskToken_Bounty_Hunter_lv3":
                    AssignTitle(player, new BountyhunterTitleLevel3());
                    titleKey = "Titles.Bountyhunter.Level3";
                    break;
                case "TaskToken_Bounty_Hunter_lv4":
                    AssignTitle(player, new BountyhunterTitleLevel4());
                    titleKey = "Titles.Bountyhunter.Level4";
                    break;
                case "TaskToken_Bounty_Hunter_lv5":
                    AssignTitle(player, new BountyhunterTitleLevel5());
                    titleKey = "Titles.Bountyhunter.Level5";
                    break;
                case "TaskToken_Wrath_lv1":
                    AssignTitle(player, new WrathTitleLevel1());
                    titleKey = "Titles.Wrath.Level1";
                    break;
                case "TaskToken_Wrath_lv2":
                    AssignTitle(player, new WrathTitleLevel2());
                    titleKey = "Titles.Wrath.Level2";
                    break;
                case "TaskToken_Wrath_lv3":
                    AssignTitle(player, new WrathTitleLevel3());
                    titleKey = "Titles.Wrath.Level3";
                    break;
                case "TaskToken_Wrath_lv4":
                    AssignTitle(player, new WrathTitleLevel4());
                    titleKey = "Titles.Wrath.Level4";
                    break;
                case "TaskToken_Wrath_lv5":
                    AssignTitle(player, new WrathTitleLevel5());
                    titleKey = "Titles.Wrath.Level5";
                    break;
                case "TaskToken_Adventurer_lv1":
                    AssignTitle(player, new AdventurerTitleLevel1());
                    titleKey = "Titles.Adventurer.Level1";
                    break;
                case "TaskToken_Adventurer_lv2":
                    AssignTitle(player, new AdventurerTitleLevel2());
                    titleKey = "Titles.Adventurer.Level2";
                    break;
                case "TaskToken_Adventurer_lv3":
                    AssignTitle(player, new AdventurerTitleLevel3());
                    titleKey = "Titles.Adventurer.Level3";
                    break;
                case "TaskToken_Adventurer_lv4":
                    AssignTitle(player, new AdventurerTitleLevel4());
                    titleKey = "Titles.Adventurer.Level4";
                    break;
                case "TaskToken_Adventurer_lv5":
                    AssignTitle(player, new AdventurerTitleLevel5());
                    titleKey = "Titles.Adventurer.Level5";
                    break;

                // PvE Tokens
                case "TaskToken_PvE_lv1":
                    GrantPvEExperience(player, 15);
                    break;
                case "TaskToken_PvE_lv2":
                    GrantPvEExperience(player, 25);
                    break;
                case "TaskToken_PvE_lv3":
                    GrantPvEExperience(player, 40);
                    break;
                case "TaskToken_PvE_lv4":
                    GrantPvEExperience(player, 60);
                    break;
                case "TaskToken_PvE_lv5":
                    GrantPvEExperience(player, 85);
                    break;
                case "TaskToken_PvE_lv6":
                    GrantPvEExperience(player, 125);
                    break;

                // PvPGvG Realm Point Tokens
                case "TaskToken_PvPGvG_lv1":
                    GrantRealmPoints(player, 15);
                    break;
                case "TaskToken_PvPGvG_lv2":
                    GrantRealmPoints(player, 25);
                    break;
                case "TaskToken_PvPGvG_lv3":
                    GrantRealmPoints(player, 45);
                    break;
                case "TaskToken_PvPGvG_lv4":
                    GrantRealmPoints(player, 70);
                    break;
                case "TaskToken_PvPGvG_lv5":
                    GrantRealmPoints(player, 100);
                    break;

                // Crafting Tokens
                case "TaskToken_Crafting_lv1":
                    GrantCraftingPoints(player, 15, 8, 4, item);
                    break;
                case "TaskToken_Crafting_lv2":
                    GrantCraftingPoints(player, 25, 13, 6, item);
                    break;
                case "TaskToken_Crafting_lv3":
                    GrantCraftingPoints(player, 45, 23, 11, item);
                    break;
                case "TaskToken_Crafting_lv4":
                    GrantCraftingPoints(player, 70, 35, 17, item);
                    break;
                case "TaskToken_Crafting_lv5":
                    GrantCraftingPoints(player, 99, 50, 25, item);
                    break;

                default:
                    return base.ReceiveItem(source, item);
            }

            if (titleKey != null)
            {
                string titleName = LanguageMgr.GetTranslation(player.Client.Account.Language, titleKey);
                string message = LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.GiveTitle", titleName);
                player.Out.SendMessage(message, eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);
            }

            player.Inventory.RemoveItem(item);
            return true;
        }

        private void AssignTitle(GamePlayer player, IPlayerTitle title)
        {
            if (!player.Titles.Contains(title))
            {
                player.Titles.Add(title);
                title.OnTitleGained(player);
                player.UpdateCurrentTitle();
            }
        }

        private void GrantPvEExperience(GamePlayer player, int percentage)
        {
            try
            {
                long xpToAdd = (player.ExperienceForNextLevel - player.ExperienceForCurrentLevel) * percentage / 100;

                while (xpToAdd > 0)
                {
                    long currentLevelXpNeeded = player.ExperienceForNextLevel - player.Experience;

                    if (xpToAdd >= currentLevelXpNeeded)
                    {
                        player.GainExperience(GameLiving.eXPSource.Quest, currentLevelXpNeeded);
                        xpToAdd -= currentLevelXpNeeded;
                    }
                    else
                    {
                        player.GainExperience(GameLiving.eXPSource.Quest, xpToAdd);
                        xpToAdd = 0;
                    }
                }
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.GainExperience", percentage), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            catch (OverflowException ex)
            {
                log.Error("OverflowException in GrantPvEExperience: " + ex.Message);
            }
        }

        private void GrantRealmPoints(GamePlayer player, int percentage)
        {
            try
            {
                long rpToAdd = ((CalculateRPsFromRealmLevel(player.RealmLevel + 1) - player.RealmPoints) * percentage / 100) * 10;

                while (rpToAdd > 0)
                {
                    long currentLevelRpNeeded = CalculateRPsFromRealmLevel(player.RealmLevel + 1) - player.RealmPoints;

                    if (rpToAdd >= currentLevelRpNeeded)
                    {
                        player.GainRealmPoints(currentLevelRpNeeded * 10);
                        rpToAdd -= currentLevelRpNeeded;
                    }
                    else
                    {
                        player.GainRealmPoints(rpToAdd * 10);
                        rpToAdd = 0;
                    }
                }

                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.GainRealmPoints", percentage), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            catch (OverflowException ex)
            {
                log.Error("OverflowException in GrantRealmPoints: " + ex.Message);
            }
        }

        protected virtual long CalculateRPsFromRealmLevel(int realmLevel)
        {
            if (realmLevel < GamePlayer.REALMPOINTS_FOR_LEVEL.Length)
                return GamePlayer.REALMPOINTS_FOR_LEVEL[realmLevel];

            return (long)(25.0 / 3.0 * (realmLevel * realmLevel * realmLevel) - 25.0 / 2.0 * (realmLevel * realmLevel) + 25.0 / 6.0 * realmLevel);
        }

        private void GrantCraftingPoints(GamePlayer player, int mainPoints, int secondaryPoints, int tertiaryPoints, InventoryItem oldItem)
        {
            if (player.CraftingPrimarySkill == eCraftingSkill.BasicCrafting)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.NoProfession"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            InventoryItem newInventoryItem = null;

            switch (oldItem.Id_nb)
            {
                case "TaskToken_Crafting_lv1":
                    newInventoryItem = CreateNewInventoryItem("Task_Parch_CraftingBuffBonus_lv1");
                    break;
                case "TaskToken_Crafting_lv2":
                    newInventoryItem = CreateNewInventoryItem("Task_Parch_CraftingBuffBonus_lv2");
                    break;
                case "TaskToken_Crafting_lv3":
                    newInventoryItem = CreateNewInventoryItem("Task_Parch_CraftingBuffBonus_lv3");
                    break;
                case "TaskToken_Crafting_lv4":
                    newInventoryItem = CreateNewInventoryItem("Task_Parch_CraftingBuffBonus_lv4");
                    break;
                case "TaskToken_Crafting_lv5":
                    newInventoryItem = CreateNewInventoryItem("Task_Parch_CraftingBuffBonus_lv5");
                    break;
            }

            if (newInventoryItem != null)
            {
                player.Inventory.RemoveItem(oldItem);
                if (!player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TextNPC.InventoryFullItemGround"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.CreateItemOnTheGround(newInventoryItem);
                }
            }

            UpdateCraftingSkills(player, mainPoints, secondaryPoints, tertiaryPoints);

            player.Out.SendUpdateCraftingSkills();
            player.SaveIntoDatabase();
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.GainCraftingPoints", mainPoints, secondaryPoints), eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        private void UpdateCraftingSkills(GamePlayer player, int mainPoints, int secondaryPoints, int tertiaryPoints)
        {
            Dictionary<eCraftingSkill, int> craftingSkills = player.CraftingSkills;
            List<eCraftingSkill> secondarySkills = new List<eCraftingSkill>();
            List<eCraftingSkill> tertiarySkills = new List<eCraftingSkill>();

            switch (player.CraftingPrimarySkill)
            {
                case eCraftingSkill.Fletching:
                    secondarySkills.Add(eCraftingSkill.WoodWorking);
                    secondarySkills.Add(eCraftingSkill.ClothWorking);
                    secondarySkills.Add(eCraftingSkill.MetalWorking);
                    secondarySkills.Add(eCraftingSkill.LeatherCrafting);
                    break;
                case eCraftingSkill.ArmorCrafting:
                case eCraftingSkill.Tailoring:
                    secondarySkills.Add(eCraftingSkill.ClothWorking);
                    secondarySkills.Add(eCraftingSkill.MetalWorking);
                    secondarySkills.Add(eCraftingSkill.LeatherCrafting);
                    tertiarySkills.Add(eCraftingSkill.SiegeCrafting);
                    break;
                case eCraftingSkill.WeaponCrafting:
                    secondarySkills.Add(eCraftingSkill.WoodWorking);
                    secondarySkills.Add(eCraftingSkill.MetalWorking);
                    secondarySkills.Add(eCraftingSkill.LeatherCrafting);
                    tertiarySkills.Add(eCraftingSkill.SiegeCrafting);
                    break;
                case eCraftingSkill.Alchemy:
                    secondarySkills.Add(eCraftingSkill.SpellCrafting);
                    secondarySkills.Add(eCraftingSkill.GemCutting);
                    secondarySkills.Add(eCraftingSkill.HerbalCrafting);
                    break;
                case eCraftingSkill.SpellCrafting:
                    secondarySkills.Add(eCraftingSkill.Alchemy);
                    secondarySkills.Add(eCraftingSkill.GemCutting);
                    secondarySkills.Add(eCraftingSkill.HerbalCrafting);
                    break;
            }

            player.GainCraftingSkill(player.CraftingPrimarySkill, mainPoints);

            foreach (var skill in secondarySkills)
            {
                player.GainCraftingSkill(skill, secondaryPoints);
            }

            foreach (var skill in tertiarySkills)
            {
                player.GainCraftingSkill(skill, tertiaryPoints);
            }
        }

        private InventoryItem CreateNewInventoryItem(string templateId)
        {
            ItemTemplate itemTemplate = GameServer.Database.FindObjectByKey<ItemTemplate>(templateId);
            if (itemTemplate != null)
            {
                return GameInventoryItem.Create(itemTemplate);
            }
            return null;
        }
    }
}
