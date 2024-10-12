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
using DOL.GS.PacketHandler;
using DOL.Language;
using System;

namespace DOL.GS.Spells
{
    [SpellHandler("Lifedrain")]
    public class LifedrainSpellHandler : DirectDamageSpellHandler
    {
        protected override void DealDamage(GameLiving target, double effectiveness)
        {
            if (target == null || !target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

            if (target is GamePlayer || target is GameNPC)
            {
                // Get Damnation values
                int casterHarmValue = m_caster.TempProperties.getProperty<int>("DamnationValue", 0);
                int targetHarmValue = target.TempProperties.getProperty<int>("DamnationValue", 0);

                bool casterIsDamned = SpellHandler.FindEffectOnTarget(m_caster, "Damnation") != null;
                bool targetIsDamned = SpellHandler.FindEffectOnTarget(target, "Damnation") != null;

                // Calculate the original damage
                AttackData ad = CalculateDamageToTarget(target, effectiveness);

                // Backup original damage and critical damage for healing later
                int originalDamage = ad.Damage;
                int originalCriticalDamage = ad.CriticalDamage;

                // Apply Damnation logic
                if (!casterIsDamned && targetIsDamned)
                {
                    if (targetHarmValue < 0)
                    {
                        ad.Damage = (ad.Damage * Math.Abs(targetHarmValue)) / 100;
                        ad.CriticalDamage = (ad.CriticalDamage * Math.Abs(targetHarmValue)) / 100;
                    }
                    else if (targetHarmValue == 0)
                    {
                        ad.Damage = 0;
                        ad.CriticalDamage = 0;
                    }
                    else if (targetHarmValue > 0)
                    {
                        // Target gets healed instead of taking damage
                        int heal = (originalDamage * targetHarmValue) / 100;
                        int criticalHeal = (originalCriticalDamage * targetHarmValue) / 100;

                        if (heal > 0)
                        {
                            target.ChangeHealth(m_caster, GameLiving.eHealthChangeType.Spell, heal);
                            MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.LifeDrain.DamnedTargetHealed", m_caster.GetPersonalizedName(target), heal), eChatType.CT_Spell);
                            MessageToLiving(target, LanguageMgr.GetTranslation((target as GamePlayer)?.Client, "SpellHandler.LifeDrain.DamnedYouHealed", heal), eChatType.CT_Spell);
                        }

                        if (criticalHeal > 0)
                        {
                            target.ChangeHealth(m_caster, GameLiving.eHealthChangeType.Spell, criticalHeal);
                            MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.LifeDrain.DamnedTargetCriticallyHealed", m_caster.GetPersonalizedName(target), criticalHeal), eChatType.CT_Spell);
                            MessageToLiving(target, LanguageMgr.GetTranslation((target as GamePlayer)?.Client, "SpellHandler.LifeDrain.DamnedYouCriticallyHealed", criticalHeal), eChatType.CT_Spell);
                        }

                        // Save the damage values to be used in StealLife before nullifying ad.Damage and ad.CriticalDamage
                        target.TempProperties.setProperty("OriginalDamage", originalDamage);
                        target.TempProperties.setProperty("OriginalCriticalDamage", originalCriticalDamage);

                        // Nullify the damage for the target but keep it for the caster in StealLife
                        ad.Damage = 0;
                        ad.CriticalDamage = 0;
                    }
                }
                else if (casterIsDamned && !targetIsDamned)
                {
                    if (casterHarmValue > 0)
                    {
                        ad.Damage += (originalDamage * casterHarmValue) / 100;
                        ad.CriticalDamage += (originalCriticalDamage * casterHarmValue) / 100;
                    }
                }
                else if (casterIsDamned && targetIsDamned)
                {
                    ad.Damage = 0;
                    ad.CriticalDamage = 0;
                }

                // Attacked living may modify the attack data.
                ad.Target.ModifyAttack(ad);

                SendDamageMessages(ad);
                DamageTarget(ad, true);
                StealLife(target, ad);  // Pass the original damage to StealLife
                target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
            }
        }

        public virtual void StealLife(GameLiving target, AttackData ad)
        {
            if (ad == null) return;
            if (!m_caster.IsAlive) return;

            int heal = (ad.Damage + ad.CriticalDamage) * m_spell.LifeDrainReturn / 100;
            int totalHealReductionPercentage = 0;

            int casterHarmValue = m_caster.TempProperties.getProperty<int>("DamnationValue", 0);
            int targetHarmValue = target.TempProperties.getProperty<int>("DamnationValue", 0);

            int originalDamage = target.TempProperties.getProperty<int>("OriginalDamage", 0);
            int originalCriticalDamage = target.TempProperties.getProperty<int>("OriginalCriticalDamage", 0);

            bool casterIsDamned = SpellHandler.FindEffectOnTarget(m_caster, "Damnation") != null;
            bool targetIsDamned = SpellHandler.FindEffectOnTarget(target, "Damnation") != null;

            if (m_caster.IsDiseased)
            {
                int amnesiaChance = m_caster.TempProperties.getProperty<int>("AmnesiaChance", 50);
                int healReductionPercentage = amnesiaChance > 0 ? amnesiaChance : 50;
                totalHealReductionPercentage += healReductionPercentage;
                if (m_caster.Health < m_caster.MaxHealth && totalHealReductionPercentage < 100 && m_caster is GamePlayer casterPlayer)
                {
                    MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "Spell.LifeTransfer.TargetDiseased", healReductionPercentage), eChatType.CT_SpellResisted);
                }
            }

            foreach (GameSpellEffect effect in m_caster.EffectList)
            {
                if (effect.SpellHandler is HealDebuffSpellHandler)
                {
                    int debuffValue = (int)effect.Spell.Value;
                    totalHealReductionPercentage += debuffValue;
                    if (m_caster.Health < m_caster.MaxHealth && totalHealReductionPercentage < 100 && m_caster is GamePlayer casterPlayer)
                    {
                        MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "HealSpellHandler.HealingReduced", debuffValue), eChatType.CT_SpellResisted);
                    }
                }
            }

            if (totalHealReductionPercentage >= 100)
            {
                totalHealReductionPercentage = 100;
                if (m_caster is GamePlayer casterPlayer)
                {
                    MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "HealSpellHandler.HealingNull"), eChatType.CT_SpellResisted);
                }
            }

            if (totalHealReductionPercentage > 0)
            {
                heal -= (heal * totalHealReductionPercentage) / 100;
            }

            if (!casterIsDamned && targetIsDamned)
            {
                if (targetHarmValue < 0)
                {
                    heal = (heal * Math.Abs(targetHarmValue)) / 100;
                    MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.LifeDrain.TargetDamnedPartiallyHealed", m_caster.GetPersonalizedName(target)), eChatType.CT_SpellResisted);
                }
                else if (targetHarmValue == 0)
                {
                    heal = 0;
                    MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.LifeDrain.DamnedNoHeal"), eChatType.CT_Important);
                }
                else if (targetHarmValue > 0)
                {
                    int damageAmount = ((originalDamage + originalCriticalDamage) * targetHarmValue) / 100;
                    heal = 0;

                    // Apply damage to caster
                    m_caster.TakeDamage(target, eDamageType.Natural, damageAmount, 0);
                    MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.LifeDrain.TargetDamnedDamaged", damageAmount), eChatType.CT_YouDied);
                    return;
                }
            }
            else if (casterIsDamned && !targetIsDamned)
            {
                if (casterHarmValue > 0)
                {
                    heal += (heal * casterHarmValue) / 100; // Increased healing
                }
            }
            else if (casterIsDamned && targetIsDamned)
            {
                // Both are damned, healing fails
                heal = 0;
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.LifeDrain.YouTargetDamnedNoHeal"), eChatType.CT_Important);
            }

            if (heal <= 0) return;

            heal = m_caster.ChangeHealth(m_caster, GameLiving.eHealthChangeType.Spell, heal);

            if (heal > 0)
            {
                if (m_caster is GamePlayer casterPlayer)
                    MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "SpellHandler.Lifedrain.Heal", heal, (heal == 1 ? "." : "s.")), eChatType.CT_Spell);
            }
            else
            {
                if (m_caster is GamePlayer casterPlayer)
                    MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "SpellHandler.Lifedrain.NoAbsorb"), eChatType.CT_SpellResisted);
            }
        }

        public LifedrainSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string ShortDescription
            => $"The target takes {Spell.Damage} Body damage and the attacker is healed for {Spell.LifeDrainReturn}% of the damage dealt.";
    }
}
