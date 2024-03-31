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
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Language;
using DOL.AI.Brain;

namespace DOL.GS.Spells
{
    [SpellHandler("DamageOverTime")]
    public class DoTSpellHandler : SpellHandler
    {
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override double GetLevelModFactor()
        {
            return 0;
        }

        protected override double CalculateAreaVariance(GameLiving target, float distance, int radius)
        {
            return 0;
        }

        public override bool IsOverwritable(GameSpellEffect compare)
        {
            return Spell.SpellType == compare.Spell.SpellType && Spell.DamageType == compare.Spell.DamageType && SpellLine.IsBaseLine == compare.SpellHandler.SpellLine.IsBaseLine;
        }

        public override AttackData CalculateDamageToTarget(GameLiving target, double effectiveness)
        {
            AttackData ad = base.CalculateDamageToTarget(target, effectiveness);
            ad.AttackType = AttackData.eAttackType.DoT;
            if (this.SpellLine.KeyName == GlobalSpellsLines.Mundane_Poisons)
            {
                RealmAbilities.L3RAPropertyEnhancer ra = Caster.GetAbility<RealmAbilities.ViperAbility>();
                if (ra != null)
                {
                    int additional = (int)((float)ad.Damage * ((float)ra.Amount / 100));
                    ad.Damage += additional;
                }
            }

            GameSpellEffect iWarLordEffect = SpellHandler.FindEffectOnTarget(target, "CleansingAura");
            if (iWarLordEffect != null)
                ad.Damage *= (int)(1.00 - (iWarLordEffect.Spell.Value * 0.01));

            //ad.CriticalDamage = 0; - DoTs can crit.
            return ad;
        }

        public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
        {
            int speclevel = 1;
            min = 1.13;
            max = 1.13;

            if (m_spellLine.KeyName == GlobalSpellsLines.Mundane_Poisons)
            {
                speclevel = m_caster.GetModifiedSpecLevel(Specs.Envenom);
                min = 1.25;
                max = 1.25;

                if (target.Level > 0)
                {
                    min = 0.25 + (speclevel - 1) / (double)target.Level;
                }
            }
            else
            {
                speclevel = m_caster.GetModifiedSpecLevel(m_spellLine.Spec);

                if (target.Level > 0)
                {
                    min = 0.13 + (speclevel - 1) / (double)target.Level;
                }
            }

            // no overspec bonus for dots

            if (min > max) min = max;
            if (min < 0) min = 0;
        }

        public override void SendDamageMessages(AttackData ad)
        {
            // Graveen: only GamePlayer should receive messages :p
            GamePlayer PlayerReceivingMessages = null;
            if (m_caster is GamePlayer)
                PlayerReceivingMessages = m_caster as GamePlayer;
            if (m_caster is GamePet)
                if ((m_caster as GamePet).Brain is IControlledBrain)
                    PlayerReceivingMessages = ((m_caster as GamePet).Brain as IControlledBrain).GetPlayerOwner();
            if (PlayerReceivingMessages == null)
                return;


            if (Caster is GamePlayer && ad.Target is GameNPC npc)
            {
                if (npc.CurrentGroupMob != null && npc.CurrentGroupMob.GroupInfos.IsInvincible == true)
                {
                    ad.Damage = 0;
                    ad.CriticalDamage = 0;
                }
            }

            if (Spell.Name.StartsWith("Proc"))
            {
                MessageToCaster(String.Format(LanguageMgr.GetTranslation(PlayerReceivingMessages.Client, "DoTSpellHandler.SendDamageMessages.YouHitFor",
                    m_caster.GetPersonalizedName(ad.Target), ad.Damage)), eChatType.CT_YouHit);
            }
            else
            {
                MessageToCaster(String.Format(LanguageMgr.GetTranslation(PlayerReceivingMessages.Client, "DoTSpellHandler.SendDamageMessages.YourHitsFor",
                    Spell.Name, m_caster.GetPersonalizedName(ad.Target), ad.Damage)), eChatType.CT_YouHit);
            }
            if (ad.CriticalDamage > 0)
                MessageToCaster(String.Format(LanguageMgr.GetTranslation(PlayerReceivingMessages.Client, "DoTSpellHandler.SendDamageMessages.YourCriticallyHits",
                    Spell.Name, m_caster.GetPersonalizedName(ad.Target), ad.CriticalDamage)), eChatType.CT_YouHit);
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target.EffectList.GetOfType<AdrenalineSpellEffect>() != null)
            {
                (m_caster as GamePlayer)?.SendTranslatedMessage("Adrenaline.Target.Immune", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow, m_caster.GetPersonalizedName(target));
                (target as GamePlayer)?.SendTranslatedMessage("Adrenaline.Self.Immune", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return;
            }
            base.ApplyEffectOnTarget(target, effectiveness);
            target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
        }


        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            // damage is not reduced with distance
            return new GameSpellEffect(this, m_spell.Duration, m_spell.Frequency, effectiveness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            SendEffectAnimation(effect.Owner, 0, false, 1);
        }

        public override void OnEffectPulse(GameSpellEffect effect)
        {
            base.OnEffectPulse(effect);

            if (effect.Owner.IsAlive)
            {
                // An acidic cloud surrounds you!
                MessageToLiving(effect.Owner, Spell.Message1, eChatType.CT_Spell);
                // {0} is surrounded by an acidic cloud!
                foreach (GamePlayer player in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                {
                    if (!(effect.Owner == player))
                    {
                        player.MessageFromArea(effect.Owner, Util.MakeSentence(Spell.Message2,
                            player.GetPersonalizedName(effect.Owner)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    }
                }

                OnDirectEffect(effect.Owner, effect.Effectiveness);
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);
            if (!noMessages)
            {
                // The acidic mist around you dissipates.
                MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
                // The acidic mist around {0} dissipates.
                foreach (GamePlayer player in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                {
                    if (!(effect.Owner == player))
                    {
                        player.MessageFromArea(effect.Owner, Util.MakeSentence(Spell.Message4,
                            player.GetPersonalizedName(effect.Owner)), eChatType.CT_SpellExpires, eChatLoc.CL_SystemWindow);
                    }
                }
            }
            return 0;
        }

        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

            // no interrupts on DoT direct effect
            // calc damage
            AttackData ad = CalculateDamageToTarget(target, effectiveness);
            SendDamageMessages(ad);
            DamageTarget(ad, false);
        }

        public DoTSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string ShortDescription => $"Target takes {Spell.Damage} {Spell.DamageType} damage every {Spell.Frequency / 1000.0} seconds.";
    }
}
