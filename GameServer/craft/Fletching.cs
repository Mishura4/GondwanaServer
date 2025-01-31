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
    public class Fletching : AbstractProfession
    {
        protected override String Profession
        {
            get
            {
                return "CraftersProfession.Fletcher";
            }
        }

        public Fletching()
        {
            Icon = 0x0C;
            Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "Crafting.Name.Fletching");
            eSkill = eCraftingSkill.Fletching;
        }

        /// <summary>
        /// Our main check for the needed tools/stations for Fletching.
        /// Includes GM bypass, inventory checks for planing tool (if desired),
        /// and lathe checks for specific item types (except Arrow / Bolt).
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
        /// Check if the player has the required tool(s) in the backpack
        /// for bows, instruments, staves, etc.
        /// (Arrows and bolts generally don't need a planing tool.)
        /// </summary>
        private bool CheckInventoryToolRequirements(GamePlayer player, Recipe recipe, eObjectType objectType)
        {
            bool HasAnyTools(params string[] toolIds)
            {
                for (int slot = (int)eInventorySlot.FirstBackpack; slot <= (int)eInventorySlot.LastBackpack; slot++)
                {
                    InventoryItem invItem = player.Inventory.GetItem((eInventorySlot)slot);
                    if (invItem == null)
                        continue;

                    string idnb = invItem.Id_nb?.ToLower();
                    if (idnb == null)
                        continue;

                    foreach (var neededId in toolIds)
                    {
                        if (idnb == neededId.ToLower())
                            return true;
                    }
                }
                return false;
            }

            if (objectType == eObjectType.Arrow || objectType == eObjectType.Bolt)
            {
                return true;
            }

            var latheNeeded = new List<eObjectType>
            {
                eObjectType.Staff,
                eObjectType.Crossbow,
                eObjectType.RecurvedBow,
                eObjectType.Longbow,
                eObjectType.Fired,
                eObjectType.Instrument,
                eObjectType.CompositeBow,
                eObjectType.MaulerStaff,
                eObjectType.Scythe
            };

            if (latheNeeded.Contains(objectType))
            {
                bool hasPlaningTool = HasAnyTools("planing_tool", "planing_tool2", "planing_tool3");
                if (!hasPlaningTool)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.FindPlaningTool", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            return true;
        }

        private bool CheckNearbyStaticItemRequirements(GamePlayer player, Recipe recipe, eObjectType objectType)
        {
            if (objectType == eObjectType.Arrow || objectType == eObjectType.Bolt)
                return true;

            var objectsInRange = player.GetItemsInRadius(CRAFT_DISTANCE);

            bool nearLathe = false;
            foreach (object obj in objectsInRange)
            {
                if (obj is GameStaticItem gsi)
                {
                    string nameLower = gsi.Name.ToLower();
                    if (nameLower == "lathe" ||
                        nameLower == "atelier de menuiserie" ||
                        gsi.Model == 481)
                    {
                        nearLathe = true;
                        break;
                    }
                }
            }

            if (!nearLathe)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                player.Out.SendMessage(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.FindLathe"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            return true;
        }

        public override int GetSecondaryCraftingSkillMinimumLevel(Recipe recipe)
        {
            switch (recipe.Product.Object_Type)
            {
                case (int)eObjectType.Fired:  //tested
                case (int)eObjectType.Longbow: //tested
                case (int)eObjectType.Crossbow: //tested
                case (int)eObjectType.Instrument: //tested
                case (int)eObjectType.RecurvedBow:
                case (int)eObjectType.CompositeBow:
                    return recipe.Level - 20;

                case (int)eObjectType.Arrow: //tested
                case (int)eObjectType.Bolt: //tested
                case (int)eObjectType.Thrown:
                    return recipe.Level - 15;

                case (int)eObjectType.Staff: //tested
                case (int)eObjectType.MaulerStaff:
                    return recipe.Level - 35;
            }

            return base.GetSecondaryCraftingSkillMinimumLevel(recipe);
        }

        public override void GainCraftingSkillPoints(GamePlayer player, Recipe recipe)
        {
            if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
            {
                player.GainCraftingSkill(eCraftingSkill.Fletching, 1);
                base.GainCraftingSkillPoints(player, recipe);
                player.Out.SendUpdateCraftingSkills();
            }
        }
    }
}
