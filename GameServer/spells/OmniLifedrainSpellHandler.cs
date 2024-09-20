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
using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    ///
    /// </summary>
    [SpellHandlerAttribute("OmniLifedrain")]
    public class OmniLifedrainSpellHandler : DirectDamageSpellHandler
    {
        /// <summary>
        /// execute direct effect
        /// </summary>
        /// <param>target that gets the damage</param>
        /// <param>factor from 0..1 (0%-100%)</param>
        public override void OnDirectEffect(GameLiving target, double effectiveness)
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
                StealLife(target, ad);
                StealEndo(ad);
                StealPower(ad);
                target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
            }
        }

        /// <summary>
        /// Uses percent of damage to heal the caster
        /// </summary>
        public virtual void StealLife(GameLiving target, AttackData ad)
        {
            if (ad == null) return;
            if (!m_caster.IsAlive) return;

            int heal = (ad.Damage + ad.CriticalDamage) * Spell.LifeDrainReturn / 100; // % factor on all drains
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
                heal -= (heal * healReductionPercentage) / 100;

                if (m_caster is GamePlayer casterPlayer)
                    MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "Spell.LifeTransfer.TargetDiseased"), eChatType.CT_SpellResisted);
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

            heal = m_caster.ChangeHealth(m_caster, GameLiving.eHealthChangeType.Spell, heal);

            if (heal > 0)
            {
                if (Caster is GamePlayer player)
                {
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client.Account.Language, "Spell.OmniLifeDrain.StealHealth", heal, (heal == 1 ? " " : "s")), eChatType.CT_Spell);
                }


                #region PVP DAMAGE
                
                if (m_caster.DamageRvRMemory > 0 && (m_caster is GamePlayer || (m_caster as NecromancerPet)?.GetLivingOwner() is not null))
                {
                    m_caster.DamageRvRMemory -= (long)Math.Max(heal, 0);
                }

                #endregion PVP DAMAGE

            }
            else
            {
                if (Caster is GamePlayer player)
                {
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client.Account.Language, "Spell.OmniLifeDrain.CannotAbsorb"), eChatType.CT_SpellResisted);
                }

                #region PVP DAMAGE
                
                if (m_caster.DamageRvRMemory > 0 && (m_caster is GamePlayer || (m_caster as NecromancerPet)?.GetLivingOwner() is not null))
                {
                    m_caster.DamageRvRMemory = 0; //Remise a zéro compteur dommages/heal rps
                }
                
                #endregion PVP DAMAGE
            }
        }
        /// <summary>
        /// Uses percent of damage to renew endurance
        /// </summary>
        public virtual void StealEndo(AttackData ad)
        {
            if (ad == null) return;
            if (!m_caster.IsAlive) return;

            int renew = ((ad.Damage + ad.CriticalDamage) * Spell.ResurrectHealth / 100) * Spell.LifeDrainReturn / 100; // %endo returned
            renew = m_caster.ChangeEndurance(m_caster, GameLiving.eEnduranceChangeType.Spell, renew);
            if (renew > 0)
            {
                if (Caster is GamePlayer player)
                {
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client.Account.Language, "Spell.OmniLifeDrain.StealEndurance", renew), eChatType.CT_Spell);
                }
            }
            else
            {
                if (Caster is GamePlayer player)
                {
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client.Account.Language, "Spell.OmniLifeDrain.CannotStealEndurance"), eChatType.CT_SpellResisted);
                }
            }
        }
        /// <summary>
        /// Uses percent of damage to replenish power
        /// </summary>
        public virtual void StealPower(AttackData ad)
        {
            if (ad == null) return;
            if (!m_caster.IsAlive) return;

            int replenish = ((ad.Damage + ad.CriticalDamage) * Spell.ResurrectMana / 100) * Spell.LifeDrainReturn / 100; // %mana returned
            replenish = m_caster.ChangeMana(m_caster, GameLiving.eManaChangeType.Spell, replenish);
            if (replenish > 0)
            {
                if (Caster is GamePlayer player)
                {
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client.Account.Language, "Spell.OmniLifeDrain.StealPower", replenish), eChatType.CT_Spell);
                }
            }
            else
            {
                if (Caster is GamePlayer player)
                {
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client.Account.Language, "Spell.OmniLifeDrain.PowerFull"), eChatType.CT_SpellResisted);
                }
            }
        }

        /// <summary>
        /// Calculates the base 100% spell damage which is then modified by damage variance factors
        /// </summary>
        /// <returns></returns>
        public override double CalculateDamageBase(GameLiving target)
        {
            double spellDamage = Spell.Damage;
            return spellDamage;
        }

        // constructor
        public OmniLifedrainSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                //Name
                list.Add("omni-lifedrain \n");
                //Description
                list.Add("Damages the target. A portion of damage is returned to the caster as health, power, and endurance.\n");
                list.Add("Damage: " + Spell.Damage);
                list.Add("Health returned: " + Spell.LifeDrainReturn + "% of damage dealt \n Power returned: " + Spell.ResurrectMana + "% of damage dealt \n Endurance returned: " + Spell.ResurrectHealth + "% of damage dealt");
                list.Add("Target: " + Spell.Target);
                if (Spell.Range != 0) list.Add("Range: " + Spell.Range);
                list.Add("Casting time: " + (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'"));
                if (Spell.DamageType != eDamageType.Natural)
                    list.Add("Damage: " + GlobalConstants.DamageTypeToName(Spell.DamageType));
                return list;
            }
        }
    }
}