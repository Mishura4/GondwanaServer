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
    [CharacterClass((int)eCharacterClass.MaulerMid, "Mauler", "Viking")]
    public class ClassMaulerMid : ClassViking
    {
        private static readonly List<PlayerRace> DefaultEligibleRaces = new()
        {
             PlayerRace.Kobold, PlayerRace.Deifrang, PlayerRace.Norseman, PlayerRace.Troll,
        };

        public ClassMaulerMid()
            : base()
        {
            m_profession = "PlayerClass.Profession.TempleofIronFist";
            m_specializationMultiplier = 15;
            m_baseHP = 600;
            m_primaryStat = eStat.STR;
            m_secondaryStat = eStat.CON;
            m_tertiaryStat = eStat.QUI;
            m_manaStat = eStat.STR;
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

        public override eClassType ClassType
        {
            get { return eClassType.Hybrid; }
        }

        public override GameTrainer.eChampionTrainerType ChampionTrainerType()
        {
            return GameTrainer.eChampionTrainerType.Viking;
        }

        public override bool HasAdvancedFromBaseClass()
        {
            return true;
        }
    }
}
