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
using System.Collections.Generic;
using DOL.GS.Realm;
using DOL.GS.Spells;

namespace DOL.GS.PlayerClass
{
    [CharacterClassAttribute((int)eCharacterClass.Savage, "Savage", "Viking")]
    public class ClassSavage : ClassViking
    {
        private static readonly List<PlayerRace> DefaultEligibleRaces = new()
        {
             PlayerRace.Dwarf, PlayerRace.Kobold, PlayerRace.Norseman, PlayerRace.Troll, PlayerRace.Valkyn,
        };

        public ClassSavage()
            : base()
        {
            m_profession = "PlayerClass.Profession.HouseofKelgor";
            m_specializationMultiplier = 15;
            m_primaryStat = eStat.DEX;
            m_secondaryStat = eStat.QUI;
            m_tertiaryStat = eStat.STR;
            m_baseWeaponSkill = 400;
            m_eligibleRaces = DefaultEligibleRaces;
            m_maxTensionFactor = 1.12f;
            m_adrenalineSpellID = AdrenalineSpellHandler.TANK_ADRENALINE_SPELL_ID;
        }

        public override int WeaponSkillFactor(eObjectType type)
        {
            return 20;
        }

        public override bool CanUseLefthandedWeapon
        {
            get { return true; }
        }

        public override bool HasAdvancedFromBaseClass()
        {
            return true;
        }
    }
}
