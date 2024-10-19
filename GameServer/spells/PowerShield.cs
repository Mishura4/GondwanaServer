﻿using DOL.GS.Spells;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.AI.Brain;
using DOL.Language;
using System;
using System.Numerics;

namespace DOL.GS.Spells
{
    [SpellHandler("PowerShield")]
    public class PowerShieldSpellHandler : SpellHandler
    {
        public PowerShieldSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine)
            : base(caster, spell, spellLine)
        {
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            GameLiving living = effect.Owner;
            GameEventMgr.AddHandler(living, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventHandler));

            if (Caster is GamePlayer casterPlayer)
            {
                MessageToLiving(casterPlayer, LanguageMgr.GetTranslation(casterPlayer.Client, "SpellHandler.PowerShield.EffectStart"), eChatType.CT_Spell);
            }

            SendEffectAnimation(effect.Owner, 0, false, 1);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameLiving living = effect.Owner;
            GameEventMgr.RemoveHandler(living, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventHandler));

            return base.OnEffectExpires(effect, noMessages);
        }

        private void EventHandler(DOLEvent e, object sender, EventArgs arguments)
        {
            if (!(arguments is AttackedByEnemyEventArgs args))
                return;

            AttackData ad = args.AttackData;
            GameLiving target = ad.Target;

            if (target == null || !target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active)
                return;

            bool isDDandDoTSpell = false;

            if (ad.AttackType == AttackData.eAttackType.Spell)
            {
                if (ad.SpellHandler != null && ad.SpellHandler.Spell != null)
                {
                    if (ad.SpellHandler.Spell.SpellType == "DirectDamage" || ad.SpellHandler.Spell.SpellType == "DamageOverTime" || ad.SpellHandler.Spell.SpellType == "Bolt" || ad.SpellHandler.Spell.SpellType == "Bomber" || ad.SpellHandler.Spell.SpellType == "Archery" || ad.SpellHandler.Spell.SpellType == "ArcheryDOT" || ad.SpellHandler.Spell.SpellType == "SiegeArrow" || ad.SpellHandler.Spell.SpellType == "HereticDamageSpeedDecreaseLOP" || ad.SpellHandler.Spell.SpellType == "HereticDoTLostOnPulse")
                    {
                        isDDandDoTSpell = true;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            bool isMeleeAttack = ad.AttackType == AttackData.eAttackType.MeleeOneHand ||
                                 ad.AttackType == AttackData.eAttackType.MeleeTwoHand ||
                                 ad.AttackType == AttackData.eAttackType.MeleeDualWield ||
                                 ad.AttackType == AttackData.eAttackType.Ranged;

            if (!isMeleeAttack && !isDDandDoTSpell)
            {
                return;
            }

            int healthThresholdPercent = (int)Spell.Damage;
            if (healthThresholdPercent == 0)
                healthThresholdPercent = 50;

            if (target.HealthPercent >= healthThresholdPercent)
                return;

            int desiredHealthPercent = (int)Spell.Value;
            if (desiredHealthPercent == 0)
                desiredHealthPercent = 80;

            int currentHealthPercent = target.HealthPercent;
            int healthToRestorePercent = desiredHealthPercent - currentHealthPercent;

            if (healthToRestorePercent <= 0)
                return;

            int maxMana = target.MaxMana;
            int currentMana = target.Mana;
            int manaPercent = (maxMana > 0) ? (currentMana * 100) / maxMana : 0;

            int adjustedHealthToRestorePercent = (healthToRestorePercent * manaPercent) / 100;

            if (adjustedHealthToRestorePercent <= 0)
                return;

            int maxHealth = target.MaxHealth;
            int healthToRestore = (adjustedHealthToRestorePercent * maxHealth) / 100;

            if (healthToRestore <= 0)
                return;

            int totalHealReductionPercentage = 0;

            if (target.IsDiseased)
            {
                int amnesiaChance = target.TempProperties.getProperty<int>("AmnesiaChance", 50);
                int healReductionPercentage = amnesiaChance > 0 ? amnesiaChance : 50;
                totalHealReductionPercentage += healReductionPercentage;
                if (target is GamePlayer playerTarget)
                {
                    if (playerTarget.Health < playerTarget.MaxHealth && totalHealReductionPercentage < 100)
                    {
                        MessageToLiving(playerTarget, LanguageMgr.GetTranslation(playerTarget.Client, "SpellHandler.HealSpell.YouDiseased", healReductionPercentage), eChatType.CT_SpellResisted);
                    }
                }
            }

            foreach (GameSpellEffect debuffEffect in target.EffectList)
            {
                if (debuffEffect.SpellHandler is HealDebuffSpellHandler)
                {
                    int debuffValue = (int)debuffEffect.Spell.Value;
                    int debuffEffectivenessBonus = 0;

                    if (target is GamePlayer gamePlayer)
                    {
                        debuffEffectivenessBonus = gamePlayer.GetModified(eProperty.DebuffEffectivness);
                    }

                    int adjustedDebuffValue = debuffValue + (debuffValue * debuffEffectivenessBonus) / 100;
                    totalHealReductionPercentage += adjustedDebuffValue;
                    if (target is GamePlayer player)
                    {
                        if (target.Health < target.MaxHealth && totalHealReductionPercentage < 100)
                        {
                            MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.HealSpell.HealingReduced", adjustedDebuffValue), eChatType.CT_SpellResisted);
                        }
                    }
                }
            }

            if (totalHealReductionPercentage > 100)
                totalHealReductionPercentage = 100;

            healthToRestore -= (healthToRestore * totalHealReductionPercentage) / 100;

            if (healthToRestore <= 0)
            {
                if (target is GamePlayer player)
                {
                    MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.HealSpell.HealingNullYou"), eChatType.CT_SpellResisted);
                }
                return;
            }

            bool applyDamnation = Spell.AmnesiaChance == 1;
            bool targetIsDamned = SpellHandler.FindEffectOnTarget(target, "Damnation") != null;

            if (applyDamnation && targetIsDamned)
            {
                int targetHarmValue = target.TempProperties.getProperty<int>("DamnationValue", 0);

                if (targetHarmValue < 0)
                {
                    healthToRestore = (healthToRestore * Math.Abs(targetHarmValue)) / 100;
                    if (target is GamePlayer player)
                    {
                        MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.HealSpell.TargetDamnedPartiallyHealed", Math.Abs(targetHarmValue)), eChatType.CT_SpellResisted);
                    }
                }
                else if (targetHarmValue == 0)
                {
                    healthToRestore = 0;
                    if (target is GamePlayer player)
                    {
                        MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.HealSpell.DamnedNoHeal"), eChatType.CT_SpellResisted);
                    }
                }
                else if (targetHarmValue > 0)
                {
                    int damageAmount = (healthToRestore * targetHarmValue) / 100;
                    target.TakeDamage(target, eDamageType.Natural, damageAmount, 0);
                    if (target is GamePlayer player)
                    {
                        MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.HealSpell.TargetDamnedDamaged", damageAmount), eChatType.CT_YouDied);
                    }
                    return;
                }
            }

            if (healthToRestore <= 0)
                return;

            target.ChangeMana(target, GameLiving.eManaChangeType.Spell, -currentMana);

            int healedAmount = target.ChangeHealth(target, GameLiving.eHealthChangeType.Spell, healthToRestore);

            if (healedAmount > 0)
            {
                if (target is GamePlayer player)
                {
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.HealSpell.TargetHealed", m_caster.GetPersonalizedName(player), healedAmount), eChatType.CT_Spell);
                    MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.HealSpell.YouAreHealed", player.GetPersonalizedName(m_caster), healedAmount), eChatType.CT_Spell);
                }
            }

            // Remove the effect after triggering
            bool cancelactiveEffect = Spell.LifeDrainReturn == 1;
            GameSpellEffect activeEffect = FindEffectOnTarget(target, "PowerShield");
            if (activeEffect != null && cancelactiveEffect)
            {
                activeEffect.Cancel(false);
            }
        }

        public override string ShortDescription
        {
            get
            {
                int healthThresholdPercent = (int)Spell.Damage;
                if (healthThresholdPercent == 0)
                    healthThresholdPercent = 50;

                int desiredHealthPercent = (int)Spell.Value;
                if (desiredHealthPercent == 0)
                    desiredHealthPercent = 80;

                return $"When the user's health is below {healthThresholdPercent}%, power will be funneled into health up to {desiredHealthPercent}% of Max Health.";
            }
        }
    }
}