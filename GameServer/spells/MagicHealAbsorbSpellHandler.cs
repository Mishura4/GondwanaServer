using DOL.GS.Spells;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.AI.Brain;
using DOL.Language;
using System;

namespace DOL.GS.Spells
{
    [SpellHandler("MagicHealAbsorb")]
    public class MagicHealAbsorbSpellHandler : SpellHandler
    {
        public MagicHealAbsorbSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine)
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
                MessageToLiving(casterPlayer, LanguageMgr.GetTranslation(casterPlayer.Client, "Languages.DBSpells.AFBuffDefAuraYou"), eChatType.CT_Spell);
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

            bool isApplicableSpell = false;

            if (ad.AttackType == AttackData.eAttackType.Spell)
            {
                if (ad.SpellHandler != null && ad.SpellHandler.Spell != null)
                {
                    string spellType = ad.SpellHandler.Spell.SpellType;
                    if (spellType == "DirectDamage" || spellType == "DamageOverTime" || spellType == "Bolt" || spellType == "Bomber" || spellType == "HereticDamageSpeedDecreaseLOP" || spellType == "HereticDoTLostOnPulse" || spellType == "DirectDamageWithDebuff" || spellType == "Lifedrain" || spellType == "OmniLifedrain")
                    {
                        isApplicableSpell = true;
                    }
                }
            }

            if (!isApplicableSpell)
            {
                return;
            }

            int absorbPercent = (int)Spell.Value;
            int remainingDamagePercent = 100 - absorbPercent;
            int originalDamage = ad.Damage + ad.CriticalDamage;

            int damageToAbsorb = (originalDamage * absorbPercent) / 100;
            int remainingDamage = originalDamage - damageToAbsorb;

            ad.Damage = (ad.Damage * remainingDamagePercent) / 100;
            ad.CriticalDamage = (ad.CriticalDamage * remainingDamagePercent) / 100;

            int healAmount = damageToAbsorb;
            int totalHealReductionPercentage = 0;

            if (target.IsDiseased)
            {
                int amnesiaChance = target.TempProperties.getProperty<int>("AmnesiaChance", 50);
                int healReductionPercentage = amnesiaChance > 0 ? amnesiaChance : 50;
                totalHealReductionPercentage += healReductionPercentage;
                if (target is GamePlayer playerTarget)
                {
                    if (target.Health < target.MaxHealth && totalHealReductionPercentage < 100)
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

            healAmount -= (healAmount * totalHealReductionPercentage) / 100;

            if (healAmount <= 0)
            {
                if (target is GamePlayer player)
                {
                    MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.HealSpell.HealingNullYou"), eChatType.CT_SpellResisted);
                }
            }
            else
            {
                bool applyDamnation = Spell.AmnesiaChance == 1;
                bool targetIsDamned = applyDamnation && SpellHandler.FindEffectOnTarget(target, "Damnation") != null;

                if (applyDamnation && targetIsDamned)
                {
                    int targetHarmValue = target.TempProperties.getProperty<int>("DamnationValue", 0);

                    if (targetHarmValue < 0)
                    {
                        healAmount = (healAmount * Math.Abs(targetHarmValue)) / 100;
                        if (target is GamePlayer player)
                        {
                            MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.HealSpell.TargetDamnedPartiallyHealed", Math.Abs(targetHarmValue)), eChatType.CT_SpellResisted);
                        }
                    }
                    else if (targetHarmValue == 0)
                    {
                        healAmount = 0;
                        if (target is GamePlayer player)
                        {
                            MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.HealSpell.DamnedNoHeal"), eChatType.CT_SpellResisted);
                        }
                    }
                    else if (targetHarmValue > 0)
                    {
                        int damageAmount = (healAmount * targetHarmValue) / 100;
                        target.TakeDamage(target, eDamageType.Natural, damageAmount, 0);
                        if (target is GamePlayer player)
                        {
                            MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.HealSpell.TargetDamnedDamaged", damageAmount), eChatType.CT_YouDied);
                        }
                        healAmount = 0;
                    }
                }

                if (healAmount > 0)
                {
                    int healedAmount = target.ChangeHealth(target, GameLiving.eHealthChangeType.Spell, healAmount);

                    if (healedAmount > 0)
                    {
                        if (target is GamePlayer player)
                        {
                            MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.HealSpell.TargetHealed", m_caster.GetPersonalizedName(player), healedAmount), eChatType.CT_Spell);
                            MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.HealSpell.YouAreHealed", player.GetPersonalizedName(m_caster), healedAmount), eChatType.CT_Spell);
                        }
                    }
                }
            }

            int additionalHealPercent = (int)Spell.Damage;

            if (additionalHealPercent > 0)
            {
                int additionalHealAmount = (originalDamage * additionalHealPercent) / 100;

                int powerHeal = additionalHealAmount / 2;
                int endoHeal = additionalHealAmount - powerHeal;
                int replenishedPower = target.ChangeMana(target, GameLiving.eManaChangeType.Spell, powerHeal);

                if (replenishedPower > 0)
                {
                    if (target is GamePlayer player)
                    {
                        MessageToCaster(LanguageMgr.GetTranslation(player.Client.Account.Language, "Spell.OmniLifeDrain.StealPower", replenishedPower), eChatType.CT_Spell);
                    }
                }
                else
                {
                    if (target is GamePlayer player)
                    {
                        MessageToCaster(LanguageMgr.GetTranslation(player.Client.Account.Language, "Spell.OmniLifeDrain.PowerFull"), eChatType.CT_SpellResisted);
                    }
                }

                int replenishedEndo = target.ChangeEndurance(target, GameLiving.eEnduranceChangeType.Spell, endoHeal);

                if (replenishedEndo > 0)
                {
                    if (target is GamePlayer player)
                    {
                        MessageToCaster(LanguageMgr.GetTranslation(player.Client.Account.Language, "Spell.OmniLifeDrain.StealEndurance", replenishedEndo), eChatType.CT_Spell);
                    }
                }
                else
                {
                    if (target is GamePlayer player)
                    {
                        MessageToCaster(LanguageMgr.GetTranslation(player.Client.Account.Language, "Spell.OmniLifeDrain.CannotStealEndurance"), eChatType.CT_SpellResisted);
                    }
                }
            }

            bool cancelactiveEffect = Spell.LifeDrainReturn == 1;
            GameSpellEffect activeEffect = FindEffectOnTarget(target, "MagicHealAbsorb");
            if (activeEffect != null && cancelactiveEffect)
            {
                activeEffect.Cancel(false);
            }
        }

        public override string ShortDescription
        {
            get
            {
                return $"The next magical attack done to you absorbs {Spell.Value}% of damages dealt and heals you instead, {Spell.Damage}% will be converted into Power and Endurance.";
            }
        }
    }
}