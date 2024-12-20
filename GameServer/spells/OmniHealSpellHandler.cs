using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Spells
{
    [SpellHandler("OmniHeal")]
    public class OmniHealSpellHandler : HealSpellHandler
    {
        public OmniHealSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }

        private int minEnd, maxEnd;
        private int minMana, maxMana;

        protected override bool ExecuteSpell(GameLiving target, bool force = false)
        {
            if (!base.ExecuteSpell(target, force))
                return false;

            CalculateEnduranceVariance(out minEnd, out maxEnd);
            CalculateManaVariance(out minMana, out maxMana);
            return true;
        }

        /// <inheritdoc />
        public override bool OnDirectEffect(GameLiving target, double effectiveness)
        {
            int amountEndu = Util.Random(minEnd, maxEnd);
            int amountMana = Util.Random(minMana, maxMana);
            if (SpellLine.KeyName == GlobalSpellsLines.Item_Effects)
            {
                amountEndu = maxEnd;
                amountMana = maxMana;
            }
            if (target.IsDiseased)
            {
                MessageTranslationToCaster("Spell.LifeTransfer.TargetDiseased2", eChatType.CT_SpellResisted);
                amountEndu /= 2;
                amountMana /= 2;
            }
            int endurance = target.ChangeEndurance(Caster, GameLiving.eEnduranceChangeType.Spell, amountEndu);
            int power = target.ChangeMana(Caster, GameLiving.eManaChangeType.Spell, amountMana);
            if (m_caster == target)
            {
                MessageTranslationToCaster("SpellHandler.OmniHeal.SelfHealed", eChatType.CT_Spell, endurance, power);
            }
            else
            {
                MessageTranslationToCaster("SpellHandler.OmniHeal.TargetHealed", eChatType.CT_Spell, target.GetName(0, false), endurance, power);
                MessageTranslationToLiving(target,"SpellHandler.OmniHeal.YouAreHealed", eChatType.CT_Spell, m_caster.GetName(0, false), endurance, power);
            }
            return true;
        }

        /// <summary>
        /// Calculates heal variance based on spec
        /// </summary>
        /// <param name="min">store min variance here</param>
        /// <param name="max">store max variance here</param>
        public virtual void CalculateManaVariance(out int min, out int max)
        {
            double spellValue = m_spell.Value;
            GamePlayer casterPlayer = m_caster as GamePlayer;

            if (m_spellLine.KeyName == GlobalSpellsLines.Item_Effects)
            {
                if (m_spell.Value > 0)
                {
                    min = (int)(spellValue * 0.75);
                    max = (int)(spellValue * 1.25);
                    return;
                }
            }

            if (m_spellLine.KeyName == GlobalSpellsLines.Potions_Effects)
            {
                if (m_spell.Value > 0)
                {
                    min = (int)(spellValue * 1.00);
                    max = (int)(spellValue * 1.25);
                    return;
                }
            }

            if (m_spellLine.KeyName == GlobalSpellsLines.Combat_Styles_Effect)
            {
                if (m_spell.Value > 0)
                {
                    if (UseMinVariance)
                    {
                        min = (int)(spellValue * 1.25);
                    }
                    else
                    {
                        min = (int)(spellValue * 0.75);
                    }

                    max = (int)(spellValue * 1.25);
                    return;
                }
            }

            if (m_spellLine.KeyName == GlobalSpellsLines.Reserved_Spells)
            {
                min = max = (int)spellValue;
                return;
            }

            // percents if less than zero
            if (spellValue < 0)
            {
                spellValue = (spellValue / -100.0) * m_caster.MaxMana;

                min = max = (int)spellValue;
                return;
            }

            int upperLimit = (int)(spellValue * 1.25);
            if (upperLimit < 1)
            {
                upperLimit = 1;
            }

            double eff = 1.25;
            double lineSpec = Caster.GetModifiedSpecLevel(m_spellLine.Spec);
            if (lineSpec < 1)
                lineSpec = 1;
            eff = 0.25;
            if (Spell.Level > 0)
            {
                eff += (lineSpec - 1.0) / Spell.Level;
                if (eff > 1.25)
                    eff = 1.25;
            }

            int lowerLimit = (int)(spellValue * eff);
            if (lowerLimit < 1)
            {
                lowerLimit = 1;
            }
            if (lowerLimit > upperLimit)
            {
                lowerLimit = upperLimit;
            }

            min = lowerLimit;
            max = upperLimit;
            return;
        }

        /// <summary>
        /// Calculates heal variance based on spec
        /// </summary>
        /// <param name="min">store min variance here</param>
        /// <param name="max">store max variance here</param>
        public virtual void CalculateEnduranceVariance(out int min, out int max)
        {
            double spellValue = m_spell.Value;
            GamePlayer casterPlayer = m_caster as GamePlayer;

            if (m_spellLine.KeyName == GlobalSpellsLines.Item_Effects)
            {
                if (m_spell.Value > 0)
                {
                    min = (int)(spellValue * 0.75);
                    max = (int)(spellValue * 1.25);
                    return;
                }
            }

            if (m_spellLine.KeyName == GlobalSpellsLines.Potions_Effects)
            {
                if (m_spell.Value > 0)
                {
                    min = (int)(spellValue * 1.00);
                    max = (int)(spellValue * 1.25);
                    return;
                }
            }

            if (m_spellLine.KeyName == GlobalSpellsLines.Combat_Styles_Effect)
            {
                if (m_spell.Value > 0)
                {
                    if (UseMinVariance)
                    {
                        min = (int)(spellValue * 1.25);
                    }
                    else
                    {
                        min = (int)(spellValue * 0.75);
                    }

                    max = (int)(spellValue * 1.25);
                    return;
                }
            }

            if (m_spellLine.KeyName == GlobalSpellsLines.Reserved_Spells)
            {
                min = max = (int)spellValue;
                return;
            }

            // percents if less than zero
            if (spellValue < 0)
            {
                spellValue = (spellValue / -100.0) * m_caster.MaxEndurance;

                min = max = (int)spellValue;
                return;
            }

            int upperLimit = (int)(spellValue * 1.25);
            if (upperLimit < 1)
            {
                upperLimit = 1;
            }

            double eff = 1.25;
            double lineSpec = Caster.GetModifiedSpecLevel(m_spellLine.Spec);
            if (lineSpec < 1)
                lineSpec = 1;
            eff = 0.25;
            if (Spell.Level > 0)
            {
                eff += (lineSpec - 1.0) / Spell.Level;
                if (eff > 1.25)
                    eff = 1.25;
            }

            int lowerLimit = (int)(spellValue * eff);
            if (lowerLimit < 1)
            {
                lowerLimit = 1;
            }
            if (lowerLimit > upperLimit)
            {
                lowerLimit = upperLimit;
            }

            min = lowerLimit;
            max = upperLimit;
            return;
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            int recastSeconds = Spell.RecastDelay / 1000;
            string mainDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.OmniHeal.MainDescription", Spell.Value);

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
}