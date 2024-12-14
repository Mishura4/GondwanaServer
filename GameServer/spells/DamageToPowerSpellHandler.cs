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
using System.Collections;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// 
    /// </summary>
    [SpellHandlerAttribute("DamageToPower")]
    public class DamageToPowerSpellHandler : LifedrainSpellHandler
    {
        /// <summary>
        /// Uses percent of damage to power the caster
        /// </summary>
        public override void StealLife(GameLiving target, AttackData ad)
        {
            if (ad == null) return;
            if (!m_caster.IsAlive) return;

            int heal = (ad.Damage + ad.CriticalDamage) * m_spell.LifeDrainReturn / 100;
            // Return the spell power? + % calculated on HP value and caster maxmana
            double manareturned = m_spell.Power + (heal * m_caster.MaxMana / 100);

            if (heal <= 0) return;
            heal = m_caster.ChangeMana(m_caster, GameLiving.eManaChangeType.Spell, (int)manareturned);

            //remove mana from target if ts's not a player
            if (!(target is GamePlayer))
                target.ChangeMana(m_caster, GameLiving.eManaChangeType.Spell, -heal);

            if (heal > 0)
            {
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.DamageToPower.PowerSteal", heal, (heal == 1 ? "." : "s.")), eChatType.CT_Spell);
            }
            else
            {
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.DamageToPower.NoMorePower"), eChatType.CT_SpellResisted);
            }
        }

        public DamageToPowerSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string ShortDescription
        {
            get
            {
                string language = Properties.SERV_LANGUAGE;
                return LanguageMgr.GetTranslation(language, "SpellDescription.DamageToPower.MainDescription", Spell.Value);
            }
        }
    }
}

