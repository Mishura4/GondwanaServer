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
using System.Collections;
using System.Reflection;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;

namespace DOL.GS
{
    public abstract class CraftNPC : GameNPC
    {
        public abstract string GUILD_ORDER { get; }

        public abstract string ACCEPTED_BY_ORDER_NAME { get; }

        public abstract string Crafters_Profession { get; }

        public abstract eCraftingSkill[] TrainedSkills { get; }

        public abstract eCraftingSkill TheCraftingSkill { get; }

        public abstract string InitialEntersentence { get; }

        protected readonly IList<int> maxValues = new List<int>(new int[] { 99, 199, 299, 399, 499, 599, 699, 799, 899, 999, 1099 });

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;
            if (player.CharacterClass == null)
                return false;

            TurnTo(player, 5000);

            if (player.Client.Account.PrivLevel == 1 && player.CraftingPrimarySkill != eCraftingSkill.BasicCrafting && player.CraftingPrimarySkill != TheCraftingSkill)
            {
                SayTo(player, eChatLoc.CL_PopupWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftNPC.Interact.NotMaster"));
                return true;
            }

            if (CheckIfPlayerNeedPromotion(player))
            {
                SayTo(player, eChatLoc.CL_ChatWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftNPC.Interact.Promoted", GetNextRang(player)));
                player.GainCraftingSkill(TheCraftingSkill, 1, true);
                player.Out.SendUpdatePlayer();
                player.Out.SendUpdateCraftingSkills();
                player.SaveIntoDatabase();
                return true;
            }


            // Dunnerholl : Basic Crafting Master does not give the option to rejoin this craft
            if (player.CraftingPrimarySkill != TheCraftingSkill && InitialEntersentence != null)
            {
                SayTo(player, eChatLoc.CL_PopupWindow, InitialEntersentence);
            }
            else if (player.CraftingPrimarySkill == TheCraftingSkill)
                SayTo(player, eChatLoc.CL_ChatWindow, "Je n'ai rien à vous apprendre pour le moment !");
            else
                // Only GM and Admin can see this one
                SayTo(player, eChatLoc.CL_PopupWindow, "Voulez-vous redevenir [Basic Crafting] ? (GM and Admin Only)");

            return true;
        }

        protected virtual string GetNextRang(GamePlayer player)
        {
            switch (player.GetCraftingSkillValue(TheCraftingSkill))
            {
                case 99:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftersTitle.JuniorApprentice", Crafters_Profession);
                case 199:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftersTitle.Apprentice", Crafters_Profession);
                case 299:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftersTitle.Neophyte", Crafters_Profession);
                case 399:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftersTitle.Assistant", Crafters_Profession);
                case 499:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftersTitle.Junior", Crafters_Profession);
                case 599:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftersTitle.Journeyman", Crafters_Profession);
                case 699:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftersTitle.Senior", Crafters_Profession);
                case 799:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftersTitle.Master", Crafters_Profession);
                case 899:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftersTitle.Grandmaster", Crafters_Profession);
                case 999:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftersTitle.Legendary", Crafters_Profession);
                case 1099:
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftersTitle.LegendaryGrandmaster", Crafters_Profession);
                default:
                    return null;
            }
        }

        protected virtual bool CheckIfPlayerNeedPromotion(GamePlayer player)
        {
            return player.CraftingPrimarySkill == TheCraftingSkill && maxValues.Contains(player.GetCraftingSkillValue(TheCraftingSkill));
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text))
                return false;
            if (source is GamePlayer == false)
                return true;

            GamePlayer player = (GamePlayer)source;

            if (text == GUILD_ORDER)
            {
                player.Out.SendCustomDialog(LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftNPC.WhisperReceive.WishToJoin", ACCEPTED_BY_ORDER_NAME), new CustomDialogResponse(CraftNpcDialogResponse));
            }
            else if (text is "respecialization" or "respécialisation")
            {
                player.Out.SendCustomDialog(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.Respec.Confirm"), new CustomDialogResponse(ResetDialogResponse));
            }
            return true;
        }

        /// <inheritdoc />
        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            if (!(source is GamePlayer player))
                return false;

            if (item.ClassType == "Token.CraftRespec")
            {
                return HandleCraftRespecToken(player, item);
            }

            if (player.CraftingPrimarySkill == eCraftingSkill.BasicCrafting)
            {
                SayTo(player, eChatLoc.CL_ChatWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.NoProfession"));
                return false;
            }

            if (player.CraftingPrimarySkill != TheCraftingSkill)
            {
                switch (item.Id_nb)
                {
                    case "TaskToken_Crafting_lv1":
                    case "TaskToken_Crafting_lv2":
                    case "TaskToken_Crafting_lv3":
                    case "TaskToken_Crafting_lv4":
                    case "TaskToken_Crafting_lv5":
                        SayTo(player, eChatLoc.CL_ChatWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftNPC.Interact.RefuseToken"));
                        break;
                    default:
                        SayTo(player, eChatLoc.CL_ChatWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftNPC.Interact.NoRespec"));
                        break;
                }
                return false;
            }

            switch (item.Id_nb)
            {
                case "TaskToken_Crafting_lv1":
                case "TaskToken_Crafting_lv2":
                case "TaskToken_Crafting_lv3":
                case "TaskToken_Crafting_lv4":
                case "TaskToken_Crafting_lv5":
                    GrantCraftingPoints(player, item);
                    break;
                default:
                    return base.ReceiveItem(source, item);
            }

            return true;
        }

        private bool HandleCraftRespecToken(GamePlayer player, InventoryItem item)
        {
            if (player.CraftingPrimarySkill == eCraftingSkill.BasicCrafting ||
                player.CraftingPrimarySkill == TheCraftingSkill)
            {
                SayTo(player, eChatLoc.CL_ChatWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftNPC.Interact.NoRespec"));
                return false;
            }
            player.Out.SendCustomDialog(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.Respec.Confirm"), new CustomDialogResponse(ResetDialogResponse));
            return false;
        }

        private void GrantCraftingPoints(GamePlayer player, InventoryItem item)
        {
            int mainPoints, secondaryPoints, tertiaryPoints;
            string newInventoryItemTemplateId;

            switch (item.Id_nb)
            {
                case "TaskToken_Crafting_lv1":
                    mainPoints = 15;
                    secondaryPoints = 8;
                    tertiaryPoints = 4;
                    newInventoryItemTemplateId = "Task_Parch_CraftingBuffBonus_lv1";
                    break;
                case "TaskToken_Crafting_lv2":
                    mainPoints = 25;
                    secondaryPoints = 13;
                    tertiaryPoints = 6;
                    newInventoryItemTemplateId = "Task_Parch_CraftingBuffBonus_lv2";
                    break;
                case "TaskToken_Crafting_lv3":
                    mainPoints = 45;
                    secondaryPoints = 23;
                    tertiaryPoints = 11;
                    newInventoryItemTemplateId = "Task_Parch_CraftingBuffBonus_lv3";
                    break;
                case "TaskToken_Crafting_lv4":
                    mainPoints = 70;
                    secondaryPoints = 35;
                    tertiaryPoints = 17;
                    newInventoryItemTemplateId = "Task_Parch_CraftingBuffBonus_lv4";
                    break;
                case "TaskToken_Crafting_lv5":
                    mainPoints = 99;
                    secondaryPoints = 50;
                    tertiaryPoints = 25;
                    newInventoryItemTemplateId = "Task_Parch_CraftingBuffBonus_lv5";
                    break;
                default:
                    return;
            }

            InventoryItem newInventoryItem = CreateNewInventoryItem(newInventoryItemTemplateId);
            if (newInventoryItem == null)
                return;

            UpdateCraftingSkills(player, mainPoints, secondaryPoints, tertiaryPoints);

            player.Inventory.RemoveItem(item);
            if (!player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TextNPC.InventoryFullItemGround"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                player.CreateItemOnTheGround(newInventoryItem);
            }

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
            var itemTemplate = GameServer.Database.FindObjectByKey<ItemTemplate>(templateId);
            if (itemTemplate == null)
                return null;

            return GameInventoryItem.Create(itemTemplate);
        }

        protected virtual void CraftNpcDialogResponse(GamePlayer player, byte response)
        {
            if (response != 0x01)
                return; //declined

            player.CraftingPrimarySkill = TheCraftingSkill;

            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftNPC.CraftNpcDialogResponse.Accepted", ACCEPTED_BY_ORDER_NAME), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            foreach (eCraftingSkill skill in TrainedSkills)
            {
                player.AddCraftingSkill(skill, 1);
            }
            player.Out.SendUpdatePlayer();
            player.Out.SendUpdateCraftingSkills();
            player.SaveIntoDatabase();
        }

        protected virtual void ResetDialogResponse(GamePlayer player, byte response)
        {
            if (response != 0x01)
                return; //declined

            foreach (InventoryItem item in player.Inventory.AllItems)
            {
                if (item.Template.ClassType == "Token.CraftRespec")
                {
                    player.ResetCraftingSkills();
                    player.Inventory.RemoveCountFromStack(item, 1);
                    Interact(player);
                    return;
                }
            }
            // No crafting token
            SayTo(player, eChatLoc.CL_PopupWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftNPC.Interact.NoToken"));
        }
    }
}