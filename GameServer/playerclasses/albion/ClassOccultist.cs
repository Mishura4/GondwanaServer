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
using DOL.GS.Realm;
using DOL.GS.Spells;
using System.Collections.Generic;

namespace DOL.GS.PlayerClass
{
    [CharacterClass((int)eCharacterClass.Occultist, "Occultist", "Disciple")]
    public class ClassOccultist : CharacterClassOccultist
    {
        private static readonly List<PlayerRace> DefaultEligibleRaces = new()
        {
             PlayerRace.Avalonian, PlayerRace.Briton, PlayerRace.Inconnu, PlayerRace.Saracen,
        };

        public ClassOccultist()
            : base()
        {
            m_profession = "PlayerClass.Profession.TempleofShadows";
            m_primaryStat = eStat.INT;
            m_secondaryStat = eStat.DEX;
            m_tertiaryStat = eStat.QUI;
            m_manaStat = eStat.INT;
            m_eligibleRaces = DefaultEligibleRaces;
            m_maxTensionFactor = 1.06f;
            m_adrenalineSpellID = AdrenalineSpellHandler.MAGE_ADRENALINE_SPELL_ID;
        }

        public override bool HasAdvancedFromBaseClass()
        {
            return true;
        }

        /// <summary>
        /// Use Tank adrenaline while under ChtonicShapeShift; Mage adrenaline otherwise.
        /// </summary>
        public override int GetAdrenalineSpellID(GamePlayer player)
        {
            return IsUnderChtonicShape(player)
                ? AdrenalineSpellHandler.TANK_ADRENALINE_SPELL_ID
                : AdrenalineSpellHandler.MAGE_ADRENALINE_SPELL_ID;
        }

        private static bool IsUnderChtonicShape(GamePlayer player)
        {
            if (player == null) return false;

            foreach (IGameEffect eff in player.EffectList)
            {
                if (eff is GameSpellEffect gse && gse.SpellHandler is ChtonicShapeShift)
                    return true;
            }
            return false;
        }
    }
}
