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
    public class WeaponCrafting : AbstractProfession
    {
        public WeaponCrafting()
        {
            Icon = 0x01;
            Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "Crafting.Name.Weaponcraft");
            eSkill = eCraftingSkill.WeaponCrafting;
        }

        protected override String Profession
        {
            get
            {
                return "CraftersProfession.Weaponcrafter";
            }
        }

        /// <summary>
        /// Checks the player's backpack tools and nearby static items (forge, lathe).
        /// Also implements GM bypass at the top if you want staff to skip checks.
        /// </summary>
        protected override bool CheckForTools(GamePlayer player, Recipe recipe)
        {
            if (player.Client.Account.PrivLevel > 1)
                return true;

            eObjectType objectType = (eObjectType)recipe.Product.Object_Type;

            if (!CheckNearbyStaticItemRequirements(player, recipe, objectType))
                return false;

            if (!Properties.ALLOW_CLASSIC_CRAFT_TOOLCHECK)
                return true;

            if (!CheckInventoryToolRequirements(player, recipe, objectType))
                return false;

            return true;
        }

        /// <summary>
        /// Check if the player has the needed backpack tool(s) 
        /// for forging certain weapon objectTypes.
        /// </summary>
        private bool CheckInventoryToolRequirements(GamePlayer player, Recipe recipe, eObjectType objectType)
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

            var forgingNeeded = new List<eObjectType>()
            {
                eObjectType.LeftAxe, eObjectType.Sword, eObjectType.Axe, eObjectType.Hammer, eObjectType.Shield,
                eObjectType.Blades, eObjectType.Piercing, eObjectType.Blunt, eObjectType.LargeWeapons,
                eObjectType.CrushingWeapon, eObjectType.SlashingWeapon, eObjectType.ThrustWeapon,
                eObjectType.TwoHandedWeapon, eObjectType.Plate, eObjectType.Scale, eObjectType.Chain
            };

            if (forgingNeeded.Contains(objectType))
            {
                bool hasHammer = HasAnyTools("smiths_hammer", "smiths_hammer2", "smiths_hammer3");
                if (!hasHammer)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.FindSmithTool", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            var forgingOrLathe = new List<eObjectType>()
            {
                eObjectType.Spear, eObjectType.HandToHand, eObjectType.CelticSpear, eObjectType.PolearmWeapon, eObjectType.FistWraps
            };
            if (forgingOrLathe.Contains(objectType))
            {
                bool hasHammer = HasAnyTools("smiths_hammer", "smiths_hammer2", "smiths_hammer3");
                bool hasPlaningTool = HasAnyTools("planing_tool", "planing_tool2", "planing_tool3");
                if (!hasHammer && !hasPlaningTool)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.FindSmithPlaningTool", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the player is near the correct static item (forge or lathe) 
        /// depending on the weapon objectType.
        /// </summary>
        private bool CheckNearbyStaticItemRequirements(GamePlayer player, Recipe recipe, eObjectType objectType)
        {
            var objectsInRange = player.GetItemsInRadius(CRAFT_DISTANCE);

            List<GameStaticItem> staticItemsInRange = new List<GameStaticItem>();
            foreach (object obj in objectsInRange)
            {
                if (obj is GameStaticItem gsi)
                    staticItemsInRange.Add(gsi);
            }

            bool IsNearForge()
            {
                return staticItemsInRange.Exists(item => item.Name.ToLower() == "forge" || item.Model == 478);
            }

            bool IsNearForgeOrLathe()
            {
                return staticItemsInRange.Exists(item =>
                    item.Name.ToLower() == "forge" ||
                    item.Name.ToLower() == "lathe" ||
                    item.Name.ToLower() == "atelier de menuiserie" ||
                    item.Model == 478 || item.Model == 481);
            }

            var forgingNeeded = new List<eObjectType>()
            {
                eObjectType.LeftAxe, eObjectType.Sword, eObjectType.Axe, eObjectType.Hammer, eObjectType.Shield,
                eObjectType.Blades, eObjectType.Piercing, eObjectType.Blunt, eObjectType.LargeWeapons,
                eObjectType.CrushingWeapon, eObjectType.SlashingWeapon, eObjectType.ThrustWeapon,
                eObjectType.TwoHandedWeapon, eObjectType.Plate, eObjectType.Scale, eObjectType.Chain
            };

            if (forgingNeeded.Contains(objectType))
            {
                if (!IsNearForge())
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.FindForge"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
                return true;
            }

            var forgingOrLathe = new List<eObjectType>()
            {
                eObjectType.Spear, eObjectType.HandToHand, eObjectType.CelticSpear, eObjectType.PolearmWeapon, eObjectType.FistWraps
            };
            if (forgingOrLathe.Contains(objectType))
            {
                if (!IsNearForgeOrLathe())
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.FindForgeLathe"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
                return true;
            }
            return true;
        }

        /// <summary>
        /// Calculate the minumum needed secondary crafting skill level to make the item
        /// </summary>
        public override int GetSecondaryCraftingSkillMinimumLevel(Recipe recipe)
        {
            switch (recipe.Product.Object_Type)
            {
                case (int)eObjectType.CrushingWeapon:
                case (int)eObjectType.SlashingWeapon:
                case (int)eObjectType.ThrustWeapon:
                case (int)eObjectType.TwoHandedWeapon:
                case (int)eObjectType.PolearmWeapon:
                case (int)eObjectType.Flexible:
                case (int)eObjectType.Sword:
                case (int)eObjectType.Hammer:
                case (int)eObjectType.Axe:
                case (int)eObjectType.Spear:
                case (int)eObjectType.HandToHand:
                case (int)eObjectType.Blades:
                case (int)eObjectType.Blunt:
                case (int)eObjectType.Piercing:
                case (int)eObjectType.LargeWeapons:
                case (int)eObjectType.CelticSpear:
                case (int)eObjectType.Scythe:
                case (int)eObjectType.FistWraps:
                    return recipe.Level - 60;
            }

            return base.GetSecondaryCraftingSkillMinimumLevel(recipe);
        }

        public override void GainCraftingSkillPoints(GamePlayer player, Recipe recipe)
        {
            if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
            {
                player.GainCraftingSkill(eCraftingSkill.WeaponCrafting, 1);
                base.GainCraftingSkillPoints(player, recipe);
                player.Out.SendUpdateCraftingSkills();
            }
        }
    }
}
