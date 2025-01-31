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
    public class ArmorCrafting : AbstractProfession
    {
        public ArmorCrafting()
        {
            Icon = 0x02;
            Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "Crafting.Name.Armorcraft");
            eSkill = eCraftingSkill.ArmorCrafting;
        }

        protected override String Profession
        {
            get
            {
                return "CraftersProfession.Armorer";
            }
        }

        protected override bool CheckForTools(GamePlayer player, Recipe recipe)
        {
            if (player.Client.Account.PrivLevel > 1)
                return true;

            eObjectType objectType = (eObjectType)recipe.Product.Object_Type;

            if (!CheckArmorStaticItemRequirements(player, recipe, objectType))
                return false;

            if (!Properties.ALLOW_CLASSIC_CRAFT_TOOLCHECK)
                return true;

            if (!CheckArmorToolRequirements(player, recipe, objectType))
                return false;

            return true;
        }

        /// <summary>
        /// Check the player's inventory for required tools when crafting certain armor types.
        /// </summary>
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

            List<eObjectType> heavyArmors = new List<eObjectType>(){eObjectType.Chain, eObjectType.Studded, eObjectType.Reinforced, eObjectType.Scale};

            if (heavyArmors.Contains(objectType))
            {
                bool hasHammer = HasAnyTools("smiths_hammer", "smiths_hammer2", "smiths_hammer3");
                bool hasSewing = HasAnyTools("sewing_kit", "sewing_kit2", "sewing_kit3");

                if (!hasHammer || !hasSewing)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.FindSmithSewingTool", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            if (objectType == eObjectType.Plate)
            {
                bool hasHammer = HasAnyTools("smiths_hammer", "smiths_hammer2", "smiths_hammer3");
                if (!hasHammer)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.FindSmithTool", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check if the player is near the right static item (forge, or weaver/tannery).
        /// </summary>
        private bool CheckArmorStaticItemRequirements(GamePlayer player, Recipe recipe, eObjectType objectType)
        {
            var objectsInRange = player.GetItemsInRadius(CRAFT_DISTANCE);

            List<GameStaticItem> staticItemsInRange = new List<GameStaticItem>();
            foreach (object obj in objectsInRange)
            {
                if (obj is GameStaticItem gsi)
                    staticItemsInRange.Add(gsi);
            }

            bool nearForge = staticItemsInRange.Exists(item =>
                item.Name.ToLower() == "forge" || item.Model == 478);

            List<eObjectType> forgingNeeded = new List<eObjectType>(){eObjectType.Plate, eObjectType.Chain, eObjectType.Scale, eObjectType.Studded, eObjectType.Reinforced };
            if (forgingNeeded.Contains(objectType))
            {
                if (!nearForge)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.FindForge"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                    return false;
                }
            }
            return true;
        }

        public override int GetSecondaryCraftingSkillMinimumLevel(Recipe recipe)
        {
            switch (recipe.Product.Object_Type)
            {
                case (int)eObjectType.Studded:
                case (int)eObjectType.Chain:
                case (int)eObjectType.Plate:
                case (int)eObjectType.Reinforced:
                case (int)eObjectType.Scale:
                    return recipe.Level - 60;
            }

            return base.GetSecondaryCraftingSkillMinimumLevel(recipe);
        }

        public override void GainCraftingSkillPoints(GamePlayer player, Recipe recipe)
        {
            if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
            {
                player.GainCraftingSkill(eCraftingSkill.ArmorCrafting, 1);
                base.GainCraftingSkillPoints(player, recipe);
                player.Out.SendUpdateCraftingSkills();
            }
        }
    }
}
