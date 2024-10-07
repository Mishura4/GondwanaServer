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
using System;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using System.Numerics;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("Tartaros")]
    public class Tartaros : LifedrainSpellHandler
    {
        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }
        /// <summary>
        /// Uses percent of damage to heal the caster
        /// </summary>
        public override void StealLife(GameLiving target, AttackData ad)
        {
            if (ad == null) return;
            if (!Caster.IsAlive) return;

            int heal = (ad.Damage + ad.CriticalDamage) * 35 / 100;
            int mana = (ad.Damage + ad.CriticalDamage) * 21 / 100;
            int endu = (ad.Damage + ad.CriticalDamage) * 14 / 100;

            if (Caster.IsDiseased)
            {
                int amnesiaChance = Caster.TempProperties.getProperty<int>("AmnesiaChance", 50);
                int healReductionPercentage = amnesiaChance > 0 ? amnesiaChance : 50;
                heal -= (heal * healReductionPercentage) / 100;
                MessageToCaster("You are diseased!", eChatType.CT_SpellResisted);
            }
            if (SpellHandler.FindEffectOnTarget(Caster, "Damnation") != null)
            {
                MessageToCaster("You are damned and cannot be healed!", eChatType.CT_SpellResisted);
                heal = 0;
            }
            if (heal <= 0) return;
            heal = Caster.ChangeHealth(Caster, GameLiving.eHealthChangeType.Spell, heal);
            if (heal > 0)
            {
                MessageToCaster("You drain " + heal + " hit point" + (heal == 1 ? "." : "s."), eChatType.CT_Spell);
            }
            else
            {
                MessageToCaster("You cannot absorb any more life.", eChatType.CT_SpellResisted);
            }

            if (mana <= 0) return;
            mana = Caster.ChangeMana(Caster, GameLiving.eManaChangeType.Spell, mana);
            if (mana > 0)
            {
                MessageToCaster("You drain " + mana + " power point" + (mana == 1 ? "." : "s."), eChatType.CT_Spell);
            }
            else
            {
                MessageToCaster("You cannot absorb any more power.", eChatType.CT_SpellResisted);
            }

            if (endu <= 0) return;
            endu = Caster.ChangeEndurance(Caster, GameLiving.eEnduranceChangeType.Spell, endu);
            if (heal > 0)
            {
                MessageToCaster("You drain " + endu + " endurance point" + (endu == 1 ? "." : "s."), eChatType.CT_Spell);
            }
            else
            {
                MessageToCaster("You cannot absorb any more endurance.", eChatType.CT_SpellResisted);
            }
        }

        public Tartaros(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
