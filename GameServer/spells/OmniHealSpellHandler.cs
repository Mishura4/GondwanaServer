using DOL.GS;
using DOL.GS.PacketHandler;
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

        public override bool StartSpell(GameLiving target)
        {
            if (!base.StartSpell(target))
                return false;
            var targets = SelectTargets(target);
            int minEnd, maxEnd;
            int minMana, maxMana;
            CalculateEnduranceVariance(out minEnd, out maxEnd);
            CalculateManaVariance(out minMana, out maxMana);
            foreach (GameLiving healTarget in targets)
            {
                int amountEndu = Util.Random(minEnd, maxEnd);
                int amountMana = Util.Random(minMana, maxMana);
                if (SpellLine.KeyName == GlobalSpellsLines.Item_Effects)
                {
                    amountEndu = maxEnd;
                    amountMana = maxMana;
                }
                if (healTarget.IsDiseased)
                {
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "Spell.LifeTransfer.TargetDiseased"), eChatType.CT_SpellResisted);
                    amountEndu /= 2;
                    amountMana /= 2;
                }
                int endurance = healTarget.ChangeEndurance(Caster, GameLiving.eEnduranceChangeType.Spell, amountEndu);
                int power = healTarget.ChangeMana(Caster, GameLiving.eManaChangeType.Spell, amountMana);
                if (m_caster == healTarget)
                {
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.OmniHeal.SelfHealed", endurance, power), eChatType.CT_Spell);
                }
                else
                {
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.OmniHeal.TargetHealed", target.GetName(0, false), endurance, power), eChatType.CT_Spell);
                    MessageToLiving(target, LanguageMgr.GetTranslation((target as GamePlayer)?.Client, "SpellHandler.OmniHeal.YouAreHealed", m_caster.GetName(0, false), endurance, power), eChatType.CT_Spell);
                }
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

        public override string ShortDescription
            => $"The target regains {Spell.Value} % of health, endurance and power.";
    }
}