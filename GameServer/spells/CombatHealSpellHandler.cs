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
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("CombatHeal")]
    public class CombatHealSpellHandler : HealSpellHandler
    {
        protected override bool ExecuteSpell(GameLiving target, bool force = false)
        {
            m_startReuseTimer = true;
            // do not start spell if not in combat
            GamePlayer player = Caster as GamePlayer;
            if (!Caster.InCombat && (player == null || player.Group == null || !player.Group.IsGroupInCombat()))
                return false;

            return base.ExecuteSpell(target, force);
        }

        public CombatHealSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        public override string GetDelveDescription(GameClient delveClient)
        {
            double freqSec = Spell.Frequency / 10.0;
            return LanguageMgr.GetTranslation(delveClient, "SpellDescription.CombatHeal.MainDescription", Spell.Value, freqSec.ToString("0.#"));
        }
    }
}
