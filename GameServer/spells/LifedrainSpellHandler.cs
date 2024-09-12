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
                // calc damage and healing
                AttackData ad = CalculateDamageToTarget(target, effectiveness);

                // Attacked living may modify the attack data.
                ad.Target.ModifyAttack(ad);

                SendDamageMessages(ad);
                DamageTarget(ad, true);
                StealLife(target, ad);
                target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
            }
        }

        public virtual void StealLife(GameLiving target, AttackData ad)
        {
            if (ad == null) return;
            if (!m_caster.IsAlive) return;

            int heal = (ad.Damage + ad.CriticalDamage) * m_spell.LifeDrainReturn / 100;
            if (m_caster.IsDiseased)
            {
                int amnesiaChance = m_caster.TempProperties.getProperty<int>("AmnesiaChance", 50);
                int healReductionPercentage = amnesiaChance > 0 ? amnesiaChance : 50;
                heal -= (heal * healReductionPercentage) / 100;

                if (m_caster is GamePlayer casterPlayer)
                    MessageToCaster(LanguageMgr.GetTranslation(casterPlayer.Client, "Spell.LifeTransfer.TargetDiseased"), eChatType.CT_SpellResisted);
            }

            if (SpellHandler.FindEffectOnTarget(m_caster, "Damnation") != null)
            {
                int harmvalue = m_caster.TempProperties.getProperty<int>("DamnationValue", 0);

                if (harmvalue > 0)
                {
                    int damageAmount = (heal * harmvalue) / 100;
                    heal = 0;

                    AttackData damageAd = new AttackData
                    {
                        Attacker = Caster,
                        Target = m_caster,
                        DamageType = eDamageType.Natural,
                        AttackType = AttackData.eAttackType.Spell,
                        Damage = damageAmount,
                        AttackResult = GameLiving.eAttackResult.HitUnstyled,
                    };
                    target.TakeDamage(damageAd);

                    MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.LifeDrain.TargetDamnedDamaged", damageAmount), eChatType.CT_YouDied);
                }
                else if (harmvalue < 0)
                {
                    heal = (heal * Math.Abs(harmvalue)) / 100;
                    MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.LifeDrain.TargetDamnedPartiallyHealed"), eChatType.CT_SpellResisted);
                }
                else
                {
                    heal = 0;
                    MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.LifeDrain.DamnedNoHeal"), eChatType.CT_Important);
                }
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
