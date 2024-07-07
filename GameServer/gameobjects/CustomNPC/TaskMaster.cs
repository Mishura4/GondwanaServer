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
using DOL.AI;
using DOL.GS.Finance;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
    public class TaskMaster : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public TextNPCCondition Condition { get; private set; }

        internal sealed class Titles
        {
            public AdventurerTitleLevel1 Adventurer1 { get; init; }

            public AdventurerTitleLevel2 Adventurer2 { get; init; }

            public AdventurerTitleLevel3 Adventurer3 { get; init; }

            public AdventurerTitleLevel4 Adventurer4 { get; init; }

            public AdventurerTitleLevel5 Adventurer5 { get; init; }

            public BountyhunterTitleLevel1 Bountyhunter1 { get; init; }

            public BountyhunterTitleLevel2 Bountyhunter2 { get; init; }

            public BountyhunterTitleLevel3 Bountyhunter3 { get; init; }

            public BountyhunterTitleLevel4 Bountyhunter4 { get; init; }

            public BountyhunterTitleLevel5 Bountyhunter5 { get; init; }

            public DemonslayerTitleLevel1 Demonslayer1 { get; init; }

            public DemonslayerTitleLevel2 Demonslayer2 { get; init; }

            public DemonslayerTitleLevel3 Demonslayer3 { get; init; }

            public DemonslayerTitleLevel4 Demonslayer4 { get; init; }

            public DemonslayerTitleLevel5 Demonslayer5 { get; init; }

            public ThiefTitleLevel1 Thief1 { get; init; }

            public ThiefTitleLevel2 Thief2 { get; init; }

            public ThiefTitleLevel3 Thief3 { get; init; }

            public ThiefTitleLevel4 Thief4 { get; init; }

            public ThiefTitleLevel5 Thief5 { get; init; }

            public TraderTitleLevel1 Trader1 { get; init; }

            public TraderTitleLevel2 Trader2 { get; init; }

            public TraderTitleLevel3 Trader3 { get; init; }

            public TraderTitleLevel4 Trader4 { get; init; }

            public TraderTitleLevel5 Trader5 { get; init; }

            public WrathTitleLevel1 Wrath1 { get; init; }

            public WrathTitleLevel2 Wrath2 { get; init; }

            public WrathTitleLevel3 Wrath3 { get; init; }

            public WrathTitleLevel4 Wrath4 { get; init; }

            public WrathTitleLevel5 Wrath5 { get; init; }

            public Titles()
            {
                foreach (PropertyInfo info in typeof(Titles).GetProperties())
                {
                    info.SetValue(this, PlayerTitleMgr.GetTitleByTypeName(info.PropertyType.FullName));
                }
            }
        }

        private static Titles titles;

        public static void LoadTitles()
        {
            titles = new Titles();
        }

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
            bool success = false;
            bool remove = true;

            switch (item.Id_nb)
            {
                case "TaskToken_Thief_lv1":
                    success = AssignTitle(player, titles.Thief1);
                    titleKey = "titles.Thief.Level1";
                    break;
                case "TaskToken_Thief_lv2":
                    success = AssignTitle(player, titles.Thief2);
                    titleKey = "titles.Thief.Level2";
                    break;
                case "TaskToken_Thief_lv3":
                    success = AssignTitle(player, titles.Thief3);
                    titleKey = "titles.Thief.Level3";
                    break;
                case "TaskToken_Thief_lv4":
                    success = AssignTitle(player, titles.Thief4);
                    titleKey = "titles.Thief.Level4";
                    break;
                case "TaskToken_Thief_lv5":
                    success = AssignTitle(player, titles.Thief5);
                    titleKey = "titles.Thief.Level5";
                    break;
                case "TaskToken_Trader_lv1":
                    success = AssignTitle(player, titles.Trader1);
                    titleKey = "titles.Trader.Level1";
                    break;
                case "TaskToken_Trader_lv2":
                    success = AssignTitle(player, titles.Trader2);
                    titleKey = "titles.Trader.Level2";
                    break;
                case "TaskToken_Trader_lv3":
                    success = AssignTitle(player, titles.Trader3);
                    titleKey = "titles.Trader.Level3";
                    break;
                case "TaskToken_Trader_lv4":
                    success = AssignTitle(player, titles.Trader4);
                    titleKey = "titles.Trader.Level4";
                    break;
                case "TaskToken_Trader_lv5":
                    success = AssignTitle(player, titles.Trader5);
                    titleKey = "titles.Trader.Level5";
                    break;
                case "TaskToken_Demon_Slayer_lv1":
                    success = AssignTitle(player, titles.Demonslayer1);
                    titleKey = "titles.Demonslayer.Level1";
                    break;
                case "TaskToken_Demon_Slayer_lv2":
                    success = AssignTitle(player, titles.Demonslayer2);
                    titleKey = "titles.Demonslayer.Level2";
                    break;
                case "TaskToken_Demon_Slayer_lv3":
                    success = AssignTitle(player, titles.Demonslayer3);
                    titleKey = "titles.Demonslayer.Level3";
                    break;
                case "TaskToken_Demon_Slayer_lv4":
                    success = AssignTitle(player, titles.Demonslayer4);
                    titleKey = "titles.Demonslayer.Level4";
                    break;
                case "TaskToken_Demon_Slayer_lv5":
                    success = AssignTitle(player, titles.Demonslayer5);
                    titleKey = "titles.Demonslayer.Level5";
                    break;
                case "TaskToken_Bounty_Hunter_lv1":
                    success = AssignTitle(player, titles.Bountyhunter1);
                    titleKey = "titles.Bountyhunter.Level1";
                    break;
                case "TaskToken_Bounty_Hunter_lv2":
                    success = AssignTitle(player, titles.Bountyhunter2);
                    titleKey = "titles.Bountyhunter.Level2";
                    break;
                case "TaskToken_Bounty_Hunter_lv3":
                    success = AssignTitle(player, titles.Bountyhunter3);
                    titleKey = "titles.Bountyhunter.Level3";
                    break;
                case "TaskToken_Bounty_Hunter_lv4":
                    success = AssignTitle(player, titles.Bountyhunter4);
                    titleKey = "titles.Bountyhunter.Level4";
                    break;
                case "TaskToken_Bounty_Hunter_lv5":
                    success = AssignTitle(player, titles.Bountyhunter5);
                    titleKey = "titles.Bountyhunter.Level5";
                    break;
                case "TaskToken_Wrath_lv1":
                    success = AssignTitle(player, titles.Wrath1);
                    titleKey = "titles.Wrath.Level1";
                    break;
                case "TaskToken_Wrath_lv2":
                    success = AssignTitle(player, titles.Wrath2);
                    titleKey = "titles.Wrath.Level2";
                    break;
                case "TaskToken_Wrath_lv3":
                    success = AssignTitle(player, titles.Wrath3);
                    titleKey = "titles.Wrath.Level3";
                    break;
                case "TaskToken_Wrath_lv4":
                    success = AssignTitle(player, titles.Wrath4);
                    titleKey = "titles.Wrath.Level4";
                    break;
                case "TaskToken_Wrath_lv5":
                    success = AssignTitle(player, titles.Wrath5);
                    titleKey = "titles.Wrath.Level5";
                    break;
                case "TaskToken_Adventurer_lv1":
                    success = AssignTitle(player, titles.Adventurer1);
                    titleKey = "titles.Adventurer.Level1";
                    break;
                case "TaskToken_Adventurer_lv2":
                    success = AssignTitle(player, titles.Adventurer2);
                    titleKey = "titles.Adventurer.Level2";
                    break;
                case "TaskToken_Adventurer_lv3":
                    success = AssignTitle(player, titles.Adventurer3);
                    titleKey = "titles.Adventurer.Level3";
                    break;
                case "TaskToken_Adventurer_lv4":
                    success = AssignTitle(player, titles.Adventurer4);
                    titleKey = "titles.Adventurer.Level4";
                    break;
                case "TaskToken_Adventurer_lv5":
                    success = AssignTitle(player, titles.Adventurer5);
                    titleKey = "titles.Adventurer.Level5";
                    break;

                // PvE Tokens
                case "TaskToken_PvE_lv1":
                    success = GrantPvEExperience(player, 15);
                    break;
                case "TaskToken_PvE_lv2":
                    success = GrantPvEExperience(player, 25);
                    break;
                case "TaskToken_PvE_lv3":
                    success = GrantPvEExperience(player, 40);
                    break;
                case "TaskToken_PvE_lv4":
                    success = GrantPvEExperience(player, 60);
                    break;
                case "TaskToken_PvE_lv5":
                    success = GrantPvEExperience(player, 85);
                    break;
                case "TaskToken_PvE_lv6":
                    success = GrantPvEExperience(player, 125);
                    break;

                // PvPGvG Realm Point Tokens
                case "TaskToken_PvPGvG_lv1":
                    success = GrantRealmPoints(player, 15);
                    break;
                case "TaskToken_PvPGvG_lv2":
                    success = GrantRealmPoints(player, 25);
                    break;
                case "TaskToken_PvPGvG_lv3":
                    success = GrantRealmPoints(player, 45);
                    break;
                case "TaskToken_PvPGvG_lv4":
                    success = GrantRealmPoints(player, 70);
                    break;
                case "TaskToken_PvPGvG_lv5":
                    success = GrantRealmPoints(player, 100);
                    break;

                // Crafting Tokens
                case "TaskToken_Crafting_lv1":
                    success = GrantCraftingPoints(player, 15, 8, 4, item);
                    remove = false;
                    break;
                case "TaskToken_Crafting_lv2":
                    success = GrantCraftingPoints(player, 25, 13, 6, item);
                    remove = false;
                    break;
                case "TaskToken_Crafting_lv3":
                    success = GrantCraftingPoints(player, 45, 23, 11, item);
                    remove = false;
                    break;
                case "TaskToken_Crafting_lv4":
                    success = GrantCraftingPoints(player, 70, 35, 17, item);
                    remove = false;
                    break;
                case "TaskToken_Crafting_lv5":
                    success = GrantCraftingPoints(player, 99, 50, 25, item);
                    remove = false;
                    break;

                default:
                    return base.ReceiveItem(source, item);
            }

            if (!success)
            {
                player.SendTranslatedMessage("TaskMaster.Disabled", eChatType.CT_Chat, eChatLoc.CL_ChatWindow);
                return true;
            }

            if (titleKey != null)
            {
                string titleName = LanguageMgr.GetTranslation(player.Client.Account.Language, titleKey);
                string message = LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.GiveTitle", titleName);
                player.Out.SendMessage(message, eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);
            }

            if (remove)
            {
                player.Inventory.RemoveItem(item);
            }
            return true;
        }

        private bool AssignTitle(GamePlayer player, IPlayerTitle title)
        {
            if (title != null && player.Titles.Add(title))
            {
                title.OnTitleGained(player);
                player.UpdateCurrentTitle();
                return true;
            }
            return false;
        }

        private bool GrantPvEExperience(GamePlayer player, int percentage)
        {
            if (!player.GainXP)
            {
                return false;
            }
            
            try
            {
                double factor = (percentage / 100.0d) * player.GetXPFactor(false);
                if (factor <= 0)
                {
                    return false;
                }
                
                if (player.Level >= player.MaxLevel)
                {
                    if (Properties.XP_TO_COPPER_RATE <= 0)
                        return false;
                    
                    // Arbitrary number for 50 to 51
                    var imaginaryXpToNextLevel = 200000000000L;
                    var copper = Finance.Money.XpToCopper((long)(Math.Round(imaginaryXpToNextLevel * factor)));
                    player.AddMoney(copper);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.GainMoney", LanguageMgr.TranslateMoneyLong(player, copper.Amount)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return true;
                }
                
                long xpNeeded = (long)Math.Round((player.ExperienceForNextLevel - player.ExperienceForCurrentLevel) * factor);

                while (xpNeeded > 0)
                {
                    long currentLevelXpNeeded = player.ExperienceForNextLevel - player.Experience;

                    if (xpNeeded >= currentLevelXpNeeded)
                    {
                        player.GainExperience(GameLiving.eXPSource.Quest, currentLevelXpNeeded, false);
                        xpNeeded -= currentLevelXpNeeded;
                    }
                    else
                    {
                        player.GainExperience(GameLiving.eXPSource.Quest, xpNeeded, false);
                        xpNeeded = 0;
                    }
                }
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.GainExperience", factor * 100), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return true;
            }
            catch (OverflowException ex)
            {
                log.Error("OverflowException in GrantPvEExperience: " + ex.Message);
                return false;
            }
        }

        private bool GrantRealmPoints(GamePlayer player, int percentage)
        {
            if (!player.GainRP)
            {
                return false;
            }
            
            try
            {
                double factor = (percentage / 100.0d) * player.GetRPFactor(false);
                if (factor <= 0)
                {
                    return false;
                }

                long rpToAdd = (long)Math.Round(player.CalculateRPsToGainRealmRank() * factor);

                player.GainRealmPoints(rpToAdd, false);

                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.GainRealmPoints", factor * 100), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return true;
            }
            catch (OverflowException ex)
            {
                log.Error("OverflowException in GrantRealmPoints: " + ex.Message);
                return false;
            }
        }

        private bool GrantCraftingPoints(GamePlayer player, int mainPoints, int secondaryPoints, int tertiaryPoints, InventoryItem oldItem)
        {
            if (player.CraftingPrimarySkill == eCraftingSkill.BasicCrafting)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.NoProfession"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
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
            return true;
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
