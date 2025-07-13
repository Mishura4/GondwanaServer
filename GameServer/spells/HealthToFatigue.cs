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
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("HealthToEndurance")]
    public class HealthToEndurance : SpellHandler
    {

        public HealthToEndurance(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override bool CheckBeginCast(GameLiving selectedTarget, bool quiet)
        {
            // Check if caster has enough endurance room
            if (m_caster.Endurance >= m_caster.MaxEndurance)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.EnduranceHeal.CasterFull"), eChatType.CT_Spell);
                return false;
            }

            // Calculate health cost (Spell.Value as percentage of MaxHealth)
            int healthCost = CalculateHealthCost();
            if (m_caster.Health < healthCost)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.HealthToEndurance.InsufficientHealth"), eChatType.CT_Spell);
                return false;
            }

            return base.CheckBeginCast(selectedTarget, quiet);
        }

        public override void FinishSpellCast(GameLiving target)
        {
            base.FinishSpellCast(target);

            // Calculate health cost and endurance gain
            int healthCost = CalculateHealthCost();
            int enduranceGain = CalculateEnduranceGain();

            // Apply disease/debuff effects (similar to OmniHeal)
            double effectiveness = 1.0;
            if (m_caster.IsDiseased)
            {
                effectiveness = 0.25; // 25% effectiveness if diseased
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.HealthToEndurance.Diseased"), eChatType.CT_SpellResisted);
            }

            // Check for Damnation effect
            var damnationEff = FindEffectOnTarget(m_caster, "Damnation");
            if (damnationEff != null)
            {
                int harmValue = m_caster.TempProperties.getProperty<int>("DamnationValue", 0);
                if (harmValue > 0)
                {
                    effectiveness = 0; // No effect if damned
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.HealthToEndurance.DamnedNoEffect"), eChatType.CT_Spell);
                    return;
                }
                else if (harmValue < 0)
                {
                    effectiveness = Math.Abs(harmValue) / 100.0; // Partial effect
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.HealthToEndurance.DamnedPartialEffect"), eChatType.CT_SpellResisted);
                }
            }

            // Deduct health
            int healthLost = m_caster.ChangeHealth(m_caster, GameLiving.eHealthChangeType.Spell, -healthCost);
            if (healthLost < 0)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.HealthToEndurance.HealthLost", Math.Abs(healthLost)), eChatType.CT_Spell);
            }

            // Apply endurance gain
            if (effectiveness > 0)
            {
                int adjustedEndurance = (int)(enduranceGain * effectiveness);
                int enduranceGained = m_caster.ChangeEndurance(m_caster, GameLiving.eEnduranceChangeType.Spell, adjustedEndurance);
                if (enduranceGained > 0)
                {
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.HealthToEndurance.EnduranceGained", enduranceGained), eChatType.CT_Spell);
                }
            }

            OnEffectExpires(null, true);
        }

        private int CalculateHealthCost()
        {
            double healthPercent = Math.Abs(m_spell.LifeDrainReturn);
            return (int)(m_caster.MaxHealth * (healthPercent / 100.0));
        }

        private int CalculateEnduranceGain()
        {
            double endurancePercent = Math.Abs(m_spell.Value);
            int enduranceGain = (int)(m_caster.MaxEndurance * (endurancePercent / 100.0));
            return Math.Min(enduranceGain, m_caster.MaxEndurance - m_caster.Endurance);
        }

        public override int CalculateEnduranceCost()
        {
            return 0;
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            int recastSeconds = Spell.RecastDelay / 1000;
            string mainDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.HealthToFatigue.MainDescription", Spell.LifeDrainReturn, Spell.Value);

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
}
