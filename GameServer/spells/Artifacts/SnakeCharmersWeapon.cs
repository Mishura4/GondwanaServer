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
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Heal Over Time spell handler
    /// </summary>
    [SpellHandlerAttribute("SnakeCharmer")]
    public class SnakeCharmer : LifedrainSpellHandler
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
            if (!m_caster.IsAlive) return;

            int heal = (ad.Damage + ad.CriticalDamage) * 50 / 100;
            int mana = (ad.Damage + ad.CriticalDamage) * 30 / 100;
            int endu = (ad.Damage + ad.CriticalDamage) * 20 / 100;
            int totalHealReductionPercentage = 0;

            if (m_caster.IsDiseased)
            {
                int amnesiaChance = m_caster.TempProperties.getProperty<int>("AmnesiaChance", 50);
                int healReductionPercentage = amnesiaChance > 0 ? amnesiaChance : 50;
                totalHealReductionPercentage += healReductionPercentage;
                if (m_caster.Health < m_caster.MaxHealth && totalHealReductionPercentage < 100)
                {
                    MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "Spell.LifeTransfer.TargetDiseased", healReductionPercentage), eChatType.CT_SpellResisted);
                }
            }

            foreach (GameSpellEffect effect in m_caster.EffectList)
            {
                if (effect.SpellHandler is HealDebuffSpellHandler)
                {
                    int debuffValue = (int)effect.Spell.Value;
                    totalHealReductionPercentage += debuffValue;
                    if (m_caster.Health < m_caster.MaxHealth && totalHealReductionPercentage < 100)
                    {
                        MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "HealSpellHandler.HealingReduced", debuffValue), eChatType.CT_SpellResisted);
                    }
                }
            }

            if (totalHealReductionPercentage >= 100)
            {
                totalHealReductionPercentage = 100;
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "HealSpellHandler.HealingNull"), eChatType.CT_SpellResisted);
            }

            if (totalHealReductionPercentage > 0)
            {
                heal -= (heal * totalHealReductionPercentage) / 100;
            }

            if (SpellHandler.FindEffectOnTarget(m_caster, "Damnation") != null)
            {
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "Damnation.Self.CannotBeHealed"), eChatType.CT_SpellResisted);
                heal = 0;
            }
            if (heal <= 0) return;
            heal = m_caster.ChangeHealth(m_caster, GameLiving.eHealthChangeType.Spell, heal);
            if (heal > 0)
            {
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.DamageSpeedDecrease.LifeSteal", heal, (heal == 1 ? "." : "s.")), eChatType.CT_Spell);
            }
            else
            {
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.DamageSpeedDecrease.NoMoreLife"), eChatType.CT_SpellResisted);
            }

            if (mana <= 0) return;
            mana = m_caster.ChangeMana(m_caster, GameLiving.eManaChangeType.Spell, mana);
            if (mana > 0)
            {
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.DamageToPower.PowerSteal", mana, (mana == 1 ? "." : "s.")), eChatType.CT_Spell);
            }
            else
            {
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.DamageToPower.NoMorePower"), eChatType.CT_SpellResisted);
            }

            if (endu <= 0) return;
            endu = m_caster.ChangeEndurance(m_caster, GameLiving.eEnduranceChangeType.Spell, endu);
            if (endu > 0)
            {
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.EnduranceDrain.StealPoints", endu, (endu == 1 ? "." : "s.")), eChatType.CT_Spell);
            }
            else
            {
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.EnduranceDrain.CannotAbsorbMore"), eChatType.CT_SpellResisted);
            }
        }

        public SnakeCharmer(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
