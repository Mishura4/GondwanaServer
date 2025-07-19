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
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.PacketHandler.Client.v168;
using DOL.GS.Styles;
using DOL.Language;
using log4net;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("StyleHandler")]
    public class StyleHandler : SpellHandler
    {
        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        public Style Style { get; protected set; }
        
        public StyleHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
            int classID = Spell.AmnesiaChance == 0 ? (Caster as GamePlayer)?.CharacterClass.ID ?? 0 : Spell.AmnesiaChance;
            Style = SkillBase.GetStyleByID((int)Spell.Value, classID);
            //Andraste - Vico : try to use classID=0 (easy way to implement CL Styles)
            if (Style == null) Style = SkillBase.GetStyleByID((int)Spell.Value, 0);
        }

        /// <inheritdoc />
        protected override bool ExecuteSpell(GameLiving target, bool force = false)
        {
            if (Style != null)
            {
                StyleProcessor.TryToUseStyle(Caster, target, Style);
                return true;
            }
            else
            {
                (Caster as GamePlayer)?.Out.SendMessage(LanguageMgr.GetTranslation((Caster as GamePlayer)!.Client, "StyleHandler.StyleNotImplemented"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return false;
            }
        }

        /// <inheritdoc />
        public override bool CheckBeginCast(GameLiving selectedTarget, bool quiet)
        {
            if (Style == null)
            {
                log.WarnFormat("Style {0} for StyleHandler by {1} ({2}) was not found!", (int)Spell.Value, Caster.Name, Caster.InternalID);
                return false;
            }
            return StyleProcessor.CanUseStyle(Caster, selectedTarget, Style, Caster.AttackWeapon);
        }

        /// <inheritdoc />
        public override bool CheckAfterCast(GameLiving target, bool quiet)
        {
            return StyleProcessor.CanUseStyle(Caster, target, Style, Caster.AttackWeapon);
        }

        /// <inheritdoc />
        public override bool CheckDuringCast(GameLiving target, bool quiet)
        {
            return StyleProcessor.CanUseStyle(Caster, target, Style, Caster.AttackWeapon);
        }

        /// <inheritdoc />
        public override bool CheckEndCast(GameLiving selectedTarget)
        {
            return StyleProcessor.CanUseStyle(Caster, selectedTarget, Style, Caster.AttackWeapon);
        }

        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();

                list.Add(Spell.Description);

                GamePlayer player = Caster as GamePlayer;

                if (player != null)
                {
                    list.Add(" ");

                    if (Style != null)
                    {
                        string description1 = LanguageMgr.GetTranslation(player.Client, "StyleHandler.UseStyle", Style.Name);
                        list.Add(description1);

                        if (Style.OpeningRequirementType > 0)
                        {
                            string openReq = GetOpeningRequirementDescription((int)Style.OpeningRequirementType, Style.OpeningRequirementValue, player.Client);
                            string description2 = LanguageMgr.GetTranslation(player.Client, "StyleHandler.NeedsRequirement", openReq);
                            list.Add(description2);
                        }

                        string description3;
                        if ((Style.WeaponTypeRequirement >= 0 && Style.WeaponTypeRequirement <= 49) || Style.WeaponTypeRequirement == 1000)
                        {
                            string weaponType = GetWeaponTypeName(Style.WeaponTypeRequirement);
                            description3 = LanguageMgr.GetTranslation(player.Client, "StyleHandler.NeedsWeapon", weaponType);
                        }
                        else if (Style.WeaponTypeRequirement == 1001)
                        {
                            description3 = LanguageMgr.GetTranslation(player.Client, "StyleHandler.AllWeaponTypes");
                        }
                        else
                        {
                            description3 = LanguageMgr.GetTranslation(player.Client, "StyleHandler.UnknownWeaponType");
                        }
                        list.Add(description3);

                        list.Add(" ");

                        DetailDisplayHandler.WriteStyleInfo(list, Style, player.Client);
                    }
                    else
                    {
                        list.Add(LanguageMgr.GetTranslation(player.Client, "StyleHandler.StyleNotFound"));
                    }
                }

                return list;
            }
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            if (Style == null)
                return LanguageMgr.GetTranslation(delveClient, "StyleHandler.StyleNotFound");

            string description1 = LanguageMgr.GetTranslation(delveClient, "StyleHandler.UseStyle", Style.Name);

            string description2 = string.Empty;
            if (Style.OpeningRequirementType > 0)
            {
                string openReq = GetOpeningRequirementDescription((int)Style.OpeningRequirementType, Style.OpeningRequirementValue, delveClient);
                description2 = LanguageMgr.GetTranslation(delveClient, "StyleHandler.NeedsRequirement", openReq);
            }

            string description3;
            if ((Style.WeaponTypeRequirement >= 0 && Style.WeaponTypeRequirement <= 49) || Style.WeaponTypeRequirement == 1000)
            {
                string weaponType = GetWeaponTypeName(Style.WeaponTypeRequirement);
                description3 = LanguageMgr.GetTranslation(delveClient, "StyleHandler.NeedsWeapon", weaponType);
            }
            else if (Style.WeaponTypeRequirement == 1001)
            {
                description3 = LanguageMgr.GetTranslation(delveClient, "StyleHandler.AllWeaponTypes");
            }
            else
            {
                description3 = LanguageMgr.GetTranslation(delveClient, "StyleHandler.UnknownWeaponType");
            }

            if (!string.IsNullOrEmpty(description2))
                return $"{description1}\n\n{description2}\n{description3}";
            else
                return $"{description1}\n\n{description3}";
        }

        private string GetWeaponTypeName(int weaponTypeId)
        {
            string specKeyName = SkillBase.ObjectTypeToSpec((eObjectType)weaponTypeId);
            if (string.IsNullOrEmpty(specKeyName))
            {
                switch (weaponTypeId)
                {
                    case 0: return "generic item";
                    case 1: return "generic weapon";
                    case 2: return "crushing weapon";
                    case 3: return "slashing weapon";
                    case 4: return "thrusting weapon";
                    case 5: return "fired weapon";
                    case 6: return "twohanded weapon";
                    case 7: return "polearm weapon";
                    case 8: return "staff weapon";
                    case 9: return "longbow weapon";
                    case 10: return "crossbow weapon";
                    case 11: return "sword weapon";
                    case 12: return "hammer weapon";
                    case 13: return "axe weapon";
                    case 14: return "spear weapon";
                    case 15: return "composite bow weapon";
                    case 16: return "thrown weapon";
                    case 17: return "left axe weapon";
                    case 18: return "recurve bow (weapon";
                    case 19: return "blades weapon";
                    case 20: return "blunt weapon";
                    case 21: return "piercing weapon";
                    case 22: return "large weapon";
                    case 23: return "celtic spear weapon";
                    case 24: return "flexible weapon";
                    case 25: return "hand to hand weapon";
                    case 26: return "scythe weapon";
                    case 27: return "fist wraps weapon";
                    case 28: return "mauler staff weapon";
                    case 31: return "generic armor";
                    case 32: return "cloth armor";
                    case 33: return "leather armor";
                    case 34: return "studded leather armor";
                    case 35: return "chain armor";
                    case 36: return "plate armor";
                    case 37: return "reinforced armor";
                    case 38: return "scale armor";
                    case 41: return "magical item";
                    case 42: return "shield armor";
                    case 43: return "arrow";
                    case 44: return "bolt";
                    case 45: return "instrument";
                    case 46: return "poison";
                    case 47: return "alchemy tincture";
                    case 48: return "spellcrafting gem";
                    case 49: return "garden object";
                    case 1000: return "Dual Wield";
                    case 1001: return "Any";
                    default: return "Unknown Weapon Type";
                }
            }

            Specialization spec = SkillBase.GetSpecialization(specKeyName);
            return spec != null ? spec.Name : "Unknown Weapon Type";
        }

        private string GetOpeningRequirementDescription(int openingType, int openingValue, GameClient client)
        {
            return LanguageMgr.GetOpeningRequirementDescription(client?.Account?.Language ?? LanguageMgr.DefaultLanguage, openingType, openingValue);
        }
    }
}

