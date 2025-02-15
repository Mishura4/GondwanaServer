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
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS
{
    public class BountyCrafting : AbstractCraftingSkill
    {
        public BountyCrafting()
        {
            Icon = 0x05;
            Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "Crafting.Name.BountyCrafing");
            eSkill = eCraftingSkill.BountyCrafting;
        }

        /// <summary>
        /// Primary check whether the player may craft the requested item.
        /// We now include:
        ///  1) Class → Allowed eObjectType
        ///  2) Required Inventory Tools
        ///  3) Required nearby GameStaticItem (forge, lathe, alchemy table, weaver/tannery)
        /// </summary>
        protected override bool CheckForTools(GamePlayer player, Recipe recipe)
        {
            if (player.Client.Account.PrivLevel > 1)
                return true;

            if (recipe.Product == null)
            {
                player.Out.SendMessage("Invalid recipe product!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            eObjectType objectType = (eObjectType)recipe.Product.Object_Type;

            if (!IsClassAllowedToCraftObjectType(player, objectType))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BountyCraft.Class.CannotCraft", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (!CheckNearbyStaticItemRequirements(player, recipe))
                return false;

            if (!Properties.ALLOW_CLASSIC_CRAFT_TOOLCHECK)
                return true;

            if (!CheckInventoryToolRequirements(player, recipe))
                return false;

            return true;
        }

        /// <summary>
        /// Class → Allowed eObjectTypes logic.
        /// Return true if the given player’s class can craft the specified objectType.
        /// </summary>
        private bool IsClassAllowedToCraftObjectType(GamePlayer player, eObjectType objectType)
        {
            List<eObjectType> allowedWeapons = GetAllowedWeaponTypesForClass((eCharacterClass)player.CharacterClass.ID);
            List<eObjectType> allowedArmor = GetAllowedArmorTypesForClass((eCharacterClass)player.CharacterClass.ID);

            bool universal = (objectType == eObjectType.Magical || objectType == eObjectType.Arrow);

            if (universal)
                return true;

            if (allowedWeapons.Contains(objectType) || allowedArmor.Contains(objectType))
                return true;

            return false;
        }

        /// <summary>
        /// Returns which weapon object types a particular class is allowed to craft.
        /// (Based on the big switch you provided.)
        /// </summary>
        private List<eObjectType> GetAllowedWeaponTypesForClass(eCharacterClass charClass)
        {
            List<eObjectType> weaponTypes = new List<eObjectType>();

            switch (charClass)
            {
                case eCharacterClass.Cabalist:
                case eCharacterClass.Necromancer:
                case eCharacterClass.WraithSummonerAlb:
                case eCharacterClass.Sorcerer:
                case eCharacterClass.Theurgist:
                case eCharacterClass.Wizard:
                case eCharacterClass.Eldritch:
                case eCharacterClass.Enchanter:
                case eCharacterClass.Mentalist:
                case eCharacterClass.Animist:
                case eCharacterClass.Bonedancer:
                case eCharacterClass.Runemaster:
                case eCharacterClass.Spiritmaster:
                    weaponTypes.Add(eObjectType.Staff);
                    break;

                case eCharacterClass.Friar:
                    weaponTypes.Add(eObjectType.Staff);
                    weaponTypes.Add(eObjectType.CrushingWeapon);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Armsman:
                    weaponTypes.Add(eObjectType.PolearmWeapon);
                    weaponTypes.Add(eObjectType.CrushingWeapon);
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.TwoHandedWeapon);
                    weaponTypes.Add(eObjectType.Crossbow);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Paladin:
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.CrushingWeapon);
                    weaponTypes.Add(eObjectType.TwoHandedWeapon);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Reaver:
                    weaponTypes.Add(eObjectType.Flexible);
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.CrushingWeapon);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Minstrel:
                    weaponTypes.Add(eObjectType.Instrument);
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Infiltrator:
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.Crossbow);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Scout:
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.Longbow);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Mercenary:
                    weaponTypes.Add(eObjectType.Fired);
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.CrushingWeapon);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Cleric:
                    weaponTypes.Add(eObjectType.CrushingWeapon);
                    weaponTypes.Add(eObjectType.Staff);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Valewalker:
                    weaponTypes.Add(eObjectType.Staff);
                    weaponTypes.Add(eObjectType.Scythe);
                    break;

                case eCharacterClass.Nightshade:
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Ranger:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.RecurvedBow);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Champion:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.LargeWeapons);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Hero:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.LargeWeapons);
                    weaponTypes.Add(eObjectType.CelticSpear);
                    weaponTypes.Add(eObjectType.Shield);
                    weaponTypes.Add(eObjectType.Fired);
                    break;

                case eCharacterClass.Blademaster:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.Fired);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Warden:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.Shield);
                    weaponTypes.Add(eObjectType.Fired);
                    break;

                case eCharacterClass.Druid:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.Shield);
                    weaponTypes.Add(eObjectType.Staff);
                    break;

                case eCharacterClass.Bard:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.Shield);
                    weaponTypes.Add(eObjectType.Instrument);
                    break;

                case eCharacterClass.Healer:
                case eCharacterClass.Shaman:
                    weaponTypes.Add(eObjectType.Staff);
                    weaponTypes.Add(eObjectType.Hammer);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Hunter:
                    weaponTypes.Add(eObjectType.Spear);
                    weaponTypes.Add(eObjectType.CompositeBow);
                    weaponTypes.Add(eObjectType.Sword);
                    break;

                case eCharacterClass.Savage:
                    weaponTypes.Add(eObjectType.HandToHand);
                    weaponTypes.Add(eObjectType.Sword);
                    weaponTypes.Add(eObjectType.Axe);
                    weaponTypes.Add(eObjectType.Hammer);
                    break;

                case eCharacterClass.Shadowblade:
                    weaponTypes.Add(eObjectType.Sword);
                    weaponTypes.Add(eObjectType.Axe);
                    weaponTypes.Add(eObjectType.LeftAxe);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Berserker:
                    weaponTypes.Add(eObjectType.LeftAxe);
                    weaponTypes.Add(eObjectType.Sword);
                    weaponTypes.Add(eObjectType.Axe);
                    weaponTypes.Add(eObjectType.Hammer);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Thane:
                case eCharacterClass.Warrior:
                    weaponTypes.Add(eObjectType.Sword);
                    weaponTypes.Add(eObjectType.Axe);
                    weaponTypes.Add(eObjectType.Hammer);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.Skald:
                    weaponTypes.Add(eObjectType.Sword);
                    weaponTypes.Add(eObjectType.Axe);
                    weaponTypes.Add(eObjectType.Hammer);
                    weaponTypes.Add(eObjectType.Shield);
                    break;

                case eCharacterClass.MaulerAlb:
                case eCharacterClass.MaulerMid:
                case eCharacterClass.MaulerHib:
                    weaponTypes.Add(eObjectType.Staff);
                    weaponTypes.Add(eObjectType.MaulerStaff);
                    weaponTypes.Add(eObjectType.FistWraps);
                    break;
            }

            return weaponTypes;
        }

        /// <summary>
        /// Returns which armor eObjectTypes a particular class is allowed to craft.
        /// (Based on the matrix you described.)
        /// </summary>
        private List<eObjectType> GetAllowedArmorTypesForClass(eCharacterClass charClass)
        {
            List<eObjectType> armorTypes = new List<eObjectType>();

            switch (charClass)
            {
                case eCharacterClass.Paladin:
                case eCharacterClass.Armsman:
                    armorTypes.Add(eObjectType.Plate);
                    armorTypes.Add(eObjectType.Chain);
                    armorTypes.Add(eObjectType.Studded);
                    armorTypes.Add(eObjectType.Leather);
                    armorTypes.Add(eObjectType.Cloth);
                    break;

                case eCharacterClass.Hero:
                case eCharacterClass.Champion:
                case eCharacterClass.Warden:
                case eCharacterClass.Druid:
                    armorTypes.Add(eObjectType.Scale);
                    armorTypes.Add(eObjectType.Reinforced);
                    armorTypes.Add(eObjectType.Leather);
                    armorTypes.Add(eObjectType.Cloth);
                    break;

                case eCharacterClass.Mercenary:
                case eCharacterClass.Cleric:
                case eCharacterClass.Reaver:
                case eCharacterClass.Thane:
                case eCharacterClass.Warrior:
                case eCharacterClass.Valkyrie:
                case eCharacterClass.Healer:
                case eCharacterClass.Shaman:
                case eCharacterClass.Skald:
                    armorTypes.Add(eObjectType.Chain);
                    armorTypes.Add(eObjectType.Studded);
                    armorTypes.Add(eObjectType.Leather);
                    armorTypes.Add(eObjectType.Cloth);
                    break;

                case eCharacterClass.Blademaster:
                case eCharacterClass.Nightshade:
                case eCharacterClass.Ranger:
                case eCharacterClass.Bard:
                case eCharacterClass.Guardian:
                    armorTypes.Add(eObjectType.Reinforced);
                    armorTypes.Add(eObjectType.Leather);
                    armorTypes.Add(eObjectType.Cloth);
                    break;

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
                    armorTypes.Add(eObjectType.Studded);
                    armorTypes.Add(eObjectType.Leather);
                    armorTypes.Add(eObjectType.Cloth);
                    break;

                case eCharacterClass.Vampiir:
                case eCharacterClass.MaulerAlb:
                case eCharacterClass.MaulerMid:
                case eCharacterClass.MaulerHib:
                case eCharacterClass.Heretic:
                case eCharacterClass.Friar:
                case eCharacterClass.Stalker:
                case eCharacterClass.MidgardRogue:
                case eCharacterClass.AlbionRogue:
                    armorTypes.Add(eObjectType.Leather);
                    armorTypes.Add(eObjectType.Cloth);
                    break;

                case eCharacterClass.Wizard:
                case eCharacterClass.Sorcerer:
                case eCharacterClass.Theurgist:
                case eCharacterClass.Necromancer:
                case eCharacterClass.WraithSummonerAlb:
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
                    armorTypes.Add(eObjectType.Cloth);
                    break;
            }

            return armorTypes;
        }

        /// <summary>
        /// Check the player’s inventory for the tool(s) needed to craft this objectType.
        /// (No items are actually removed; we only confirm presence.)
        /// </summary>
        private bool CheckInventoryToolRequirements(GamePlayer player, Recipe recipe)
        {
            eObjectType objectType = (eObjectType)recipe.Product.Object_Type;

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

            if (objectType == eObjectType.Magical)
            {
                if (!HasAnyTools("spellcraft_kit", "spellcraft_kit2", "spellcraft_kit3"))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.FindSpellcraftKit", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            List<eObjectType> metalWeapons = new List<eObjectType>
            {
                eObjectType.LeftAxe,
                eObjectType.Sword,
                eObjectType.Axe,
                eObjectType.Hammer,
                eObjectType.Shield,
                eObjectType.Blades,
                eObjectType.Piercing,
                eObjectType.Blunt,
                eObjectType.LargeWeapons,
                eObjectType.CrushingWeapon,
                eObjectType.SlashingWeapon,
                eObjectType.ThrustWeapon,
                eObjectType.TwoHandedWeapon,
                eObjectType.Plate
            };

            if (metalWeapons.Contains(objectType))
            {
                if (!HasAnyTools("smiths_hammer", "smiths_hammer2", "smiths_hammer3"))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.FindSmithTool", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            List<eObjectType> spearOrPolearm = new List<eObjectType>
            {
                eObjectType.Spear,
                eObjectType.HandToHand,
                eObjectType.CelticSpear,
                eObjectType.PolearmWeapon,
                eObjectType.FistWraps
            };
            if (spearOrPolearm.Contains(objectType))
            {
                bool hasHammer = HasAnyTools("smiths_hammer", "smiths_hammer2", "smiths_hammer3");
                bool hasPlaningTool = HasAnyTools("planing_tool", "planing_tool2", "planing_tool3");

                if (!hasHammer && !hasPlaningTool)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.FindSmithPlaningTool", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            List<eObjectType> latheGroup = new List<eObjectType>
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
            if (latheGroup.Contains(objectType))
            {
                if (!HasAnyTools("planing_tool", "planing_tool2", "planing_tool3"))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.FindPlaningTool", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            if (objectType == eObjectType.Leather || objectType == eObjectType.Cloth)
            {
                if (!HasAnyTools("sewing_kit", "sewing_kit2", "sewing_kit3"))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.FindSewingKit", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            List<eObjectType> heavyArmors = new List<eObjectType>
            {
                eObjectType.Scale,
                eObjectType.Chain,
                eObjectType.Reinforced,
                eObjectType.Studded
            };
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

            return true;
        }

        /// <summary>
        /// Check whether the player is near the required GameStaticItem 
        /// (forge, lathe, weaver, tannery, alchemy table, etc.).
        /// </summary>
        private bool CheckNearbyStaticItemRequirements(GamePlayer player, Recipe recipe)
        {
            eObjectType objectType = (eObjectType)recipe.Product.Object_Type;
            var objectsInRange = player.GetItemsInRadius(CRAFT_DISTANCE);

            List<GameStaticItem> staticItemsInRange = new List<GameStaticItem>();
            foreach (object obj in objectsInRange)
            {
                if (obj is GameStaticItem gsi)
                {
                    staticItemsInRange.Add(gsi);
                }
            }

            bool IsNearForge()
            {
                return staticItemsInRange.Any(item => item.Name.ToLower() == "forge" || item.Model == 478);
            }

            bool IsNearLathe()
            {
                return staticItemsInRange.Any(item =>
                    item.Name.ToLower() == "lathe" ||
                    item.Name.ToLower() == "atelier de menuiserie" ||
                    item.Model == 481);
            }

            bool IsNearForgeOrLathe()
            {
                return staticItemsInRange.Any(item =>
                    item.Name.ToLower() == "forge" ||
                    item.Name.ToLower() == "lathe" ||
                    item.Name.ToLower() == "atelier de menuiserie" ||
                    item.Model == 478 || item.Model == 481);
            }

            bool IsNearAlchemyTable()
            {
                return staticItemsInRange.Any(item =>
                    item.Name.ToLower() == "alchemy table" ||
                    item.Name.ToLower() == "table d'alchimie" ||
                    item.Model == 820);
            }

            bool IsNearWeaverOrTannery()
            {
                return staticItemsInRange.Any(item =>
                    item.Name.ToLower() == "weaver" ||
                    item.Name.ToLower() == "tannery" ||
                    item.Name.ToLower() == "fileuse" ||
                    item.Name.ToLower() == "tannerie" ||
                    item.Model == 479 ||
                    item.Model == 480
                );
            }

            var forgingNeeded = new List<eObjectType>
            {
                eObjectType.LeftAxe, eObjectType.Sword, eObjectType.Axe, eObjectType.Hammer, eObjectType.Shield,
                eObjectType.Blades, eObjectType.Piercing, eObjectType.Blunt, eObjectType.LargeWeapons,
                eObjectType.CrushingWeapon, eObjectType.SlashingWeapon, eObjectType.ThrustWeapon,
                eObjectType.TwoHandedWeapon, eObjectType.Plate, eObjectType.Scale, eObjectType.Chain
            };

            var forgingOrLathe = new List<eObjectType>
            {
                eObjectType.Spear, eObjectType.HandToHand, eObjectType.CelticSpear, eObjectType.PolearmWeapon, eObjectType.FistWraps
            };

            var latheNeeded = new List<eObjectType>
            {
                eObjectType.Staff, eObjectType.Crossbow, eObjectType.RecurvedBow, eObjectType.Fired, eObjectType.MaulerStaff,
                eObjectType.Instrument, eObjectType.CompositeBow, eObjectType.Scythe, eObjectType.Longbow
            };

            var weaverTanneryNeeded = new List<eObjectType>
            {
                eObjectType.Reinforced, eObjectType.Studded, eObjectType.Leather, eObjectType.Cloth
            };

            if (objectType == eObjectType.Magical)
            {
                if (!IsNearAlchemyTable())
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(Properties.DB_LANGUAGE, "Crafting.CheckTool.FindAlchemyTable"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
                return true;
            }

            if (forgingNeeded.Contains(objectType))
            {
                if (!IsNearForge())
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(Properties.DB_LANGUAGE, "Crafting.CheckTool.FindForge"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
                return true;
            }

            if (forgingOrLathe.Contains(objectType))
            {
                if (!IsNearForgeOrLathe())
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(Properties.DB_LANGUAGE, "Crafting.CheckTool.FindForgeLathe"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
                return true;
            }

            if (latheNeeded.Contains(objectType))
            {
                if (!IsNearLathe())
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(Properties.DB_LANGUAGE, "Crafting.CheckTool.FindLathe"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
                return true;
            }

            if (weaverTanneryNeeded.Contains(objectType))
            {
                if (!Properties.ALLOW_CLASSIC_CRAFT_TOOLCHECK)
                    return true;

                if (!IsNearWeaverOrTannery())
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Crafting.CheckTool.NotHaveTools", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(Properties.DB_LANGUAGE, "Crafting.CheckTool.FindWeaverTannery"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
                return true;
            }

            return true;
        }

        public override void GainCraftingSkillPoints(GamePlayer player, Recipe recipe)
        {
        }

        public override int CalculateChanceToMakeItem(GamePlayer player, int craftingLevel)
        {
            return 100;
        }
    }
}
