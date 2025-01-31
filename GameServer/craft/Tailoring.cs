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
using DOL.Database;
using DOL.Language;
using DOL.GS.PacketHandler;
using System;
using System.Collections.Generic;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
    public class Tailoring : AbstractProfession
    {
        public Tailoring()
        {
            Icon = 0x0B;
            Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "Crafting.Name.Tailoring");
            eSkill = eCraftingSkill.Tailoring;
        }

        protected override String Profession
        {
            get
            {
                return "CraftersProfession.Tailor";
            }
        }

        protected override bool CheckForTools(GamePlayer player, Recipe recipe)
        {
            if (player.Client.Account.PrivLevel > 1)
                return true;

            eObjectType objectType = (eObjectType)recipe.Product.Object_Type;

            if (!Properties.ALLOW_CLASSIC_CRAFT_TOOLCHECK && recipe.Product.Object_Type != (int)eObjectType.Studded && recipe.Product.Object_Type != (int)eObjectType.Reinforced)
                return true;

            if (!CheckArmorStaticItemRequirements(player, recipe, objectType))
                return false;

            if (!CheckArmorToolRequirements(player, recipe, objectType))
                return false;

            return true;
        }

        private bool CheckArmorToolRequirements(GamePlayer player, Recipe recipe, eObjectType objectType)
        {
            bool HasAnyTools(params string[] toolIdNbs)
            {
                for (int slot = (int)eInventorySlot.FirstBackpack; slot <= (int)eInventorySlot.LastBackpack; slot++)
                {
                    InventoryItem invItem = player.Inventory.GetItem((eInventorySlot)slot);
                    if (invItem == null)
                        continue;

                    string itemIdNb = invItem.Id_nb?.ToLower();
                    if (itemIdNb == null)
                        continue;

                    foreach (var neededId in toolIdNbs)
                    {
                        if (itemIdNb == neededId.ToLower())
                            return true;
                    }
                }
                return false;
            }

            List<eObjectType> mediumArmors = new List<eObjectType>() { eObjectType.Studded, eObjectType.Reinforced };
            if (mediumArmors.Contains(objectType))
            {
                bool hasHammer = HasAnyTools("smiths_hammer", "smiths_hammer2", "smiths_hammer3");
                bool hasSewing = HasAnyTools("sewing_kit", "sewing_kit2", "sewing_kit3");

                if (!hasHammer || !hasSewing)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.FindSmithSewingTool", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            List<eObjectType> lightArmors = new List<eObjectType>() { eObjectType.Leather, eObjectType.Cloth, eObjectType.Magical };
            if (lightArmors.Contains(objectType))
            {
                bool hasSewing = HasAnyTools("sewing_kit", "sewing_kit2", "sewing_kit3");
                if (!hasSewing)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.FindSewingKit", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }
            return true;
        }

        private bool CheckArmorStaticItemRequirements(GamePlayer player, Recipe recipe, eObjectType objectType)
        {
            var objectsInRange = player.GetItemsInRadius(CRAFT_DISTANCE);

            List<GameStaticItem> staticItemsInRange = new List<GameStaticItem>();
            foreach (object obj in objectsInRange)
            {
                if (obj is GameStaticItem gsi)
                    staticItemsInRange.Add(gsi);
            }

            bool nearForge = staticItemsInRange.Exists(item => item.Name.ToLower() == "forge" || item.Model == 478);

            bool nearWeaverTannery = staticItemsInRange.Exists(item =>
                item.Name.ToLower() == "weaver" || item.Name.ToLower() == "tannery" ||
                item.Name.ToLower() == "fileuse" || item.Name.ToLower() == "tannerie" ||
                item.Model == 479 || item.Model == 480);

            List<eObjectType> forgingNeeded = new List<eObjectType>() { eObjectType.Studded, eObjectType.Reinforced };
            if (forgingNeeded.Contains(objectType))
            {
                if (!nearForge)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.FindForge"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                    return false;
                }
            }

            List<eObjectType> weaverNeeded = new List<eObjectType>() { eObjectType.Leather, eObjectType.Cloth };
            if (weaverNeeded.Contains(objectType))
            {
                if (!nearWeaverTannery)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.FindWeaverTannery"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                    return false;
                }
            }
            return true;
        }

        public override int GetSecondaryCraftingSkillMinimumLevel(Recipe recipe)
        {
            switch (recipe.Product.Object_Type)
            {
                case (int)eObjectType.Cloth:
                case (int)eObjectType.Leather:
                case (int)eObjectType.Studded:
                    return recipe.Level - 30;
            }

            return base.GetSecondaryCraftingSkillMinimumLevel(recipe);
        }

        public override void GainCraftingSkillPoints(GamePlayer player, Recipe recipe)
        {
            if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
            {
                player.GainCraftingSkill(eCraftingSkill.Tailoring, 1);
                base.GainCraftingSkillPoints(player, recipe);
                player.Out.SendUpdateCraftingSkills();
            }
        }
    }
}
