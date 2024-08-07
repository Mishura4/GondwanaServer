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
using System.Text;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    [SpellHandler("UnbreakableSpeedDecrease")]
    public class UnbreakableSpeedDecreaseSpellHandler : ImmunityEffectSpellHandler
    {
        /// <inheritdoc />
        public override bool HasPositiveEffect => false;

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target.HasAbility(Abilities.CCImmunity) || target.HasAbility(Abilities.RootImmunity))
            {
                MessageToCaster(m_caster.GetPersonalizedName(target) + " is immune to this effect!", eChatType.CT_SpellResisted);
                return;
            }
            if (target.EffectList.GetOfType<AdrenalineSpellEffect>() != null)
            {
                (m_caster as GamePlayer)?.SendTranslatedMessage("Adrenaline.Target.Immune", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow, m_caster.GetPersonalizedName(target));
                (target as GamePlayer)?.SendTranslatedMessage("Adrenaline.Self.Immune", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return;
            }
            if (target.EffectList.GetOfType<ChargeEffect>() != null)
            {
                MessageToCaster(m_caster.GetPersonalizedName(target) + " is moving to fast for this spell to have any effect!", eChatType.CT_SpellResisted);
                return;
            }

            base.ApplyEffectOnTarget(target, effectiveness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect, 1.0 - Spell.Value * 0.01);

            SendUpdates(effect.Owner);

            MessageToLiving(effect.Owner, Spell.Message1, eChatType.CT_Spell);
            foreach (GamePlayer player in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            {
                if (!(effect.Owner == player))
                {
                    player.MessageFromArea(effect.Owner, Util.MakeSentence(Spell.Message2,
                        player.GetPersonalizedName(effect.Owner)), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                }
            }


            RestoreSpeedTimer timer = new RestoreSpeedTimer(effect);
            effect.Owner.TempProperties.setProperty(effect, timer);
            timer.Interval = 650;
            timer.Start(1 + (effect.Duration >> 1));

            effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);

            GameTimer timer = (GameTimer)effect.Owner.TempProperties.getProperty<object>(effect, null);
            effect.Owner.TempProperties.removeProperty(effect);
            if (timer != null) timer.Stop();

            effect.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, effect);

            SendUpdates(effect.Owner);

            MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
            foreach (GamePlayer player in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            {
                if (!(effect.Owner == player))
                {
                    player.MessageFromArea(effect.Owner, Util.MakeSentence(Spell.Message4,
                        player.GetPersonalizedName(effect.Owner)), eChatType.CT_SpellExpires, eChatLoc.CL_SystemWindow);
                }
            }

            return 60000;
        }

        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            double duration = base.CalculateEffectDuration(target, effectiveness);
            duration *= target.GetModified(eProperty.SpeedDecreaseDurationReduction) * 0.01;

            if (duration < 1)
                duration = 1;
            else if (duration > (Spell.Duration * 4))
                duration = (Spell.Duration * 4);
            return (int)duration;
        }

        protected static void SendUpdates(GameLiving owner)
        {
            if (owner.IsMezzed || owner.IsStunned)
                return;

            owner.UpdateMaxSpeed();
        }

        private sealed class RestoreSpeedTimer : GameTimer
        {
            private readonly GameSpellEffect m_effect;

            public RestoreSpeedTimer(GameSpellEffect effect) : base(effect.Owner.CurrentRegion.TimeManager)
            {
                m_effect = effect;
            }

            protected override void OnTick()
            {
                GameSpellEffect effect = m_effect;

                double factor = 2.0 - (effect.Duration - effect.RemainingTime) / (double)(effect.Duration >> 1);
                if (factor < 0) factor = 0;
                else if (factor > 1) factor = 1;

                effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect, 1.0 - effect.Spell.Value * factor * 0.01);

                SendUpdates(effect.Owner);

                if (factor <= 0)
                    Stop();
            }

            public override string ToString()
            {
                return new StringBuilder(base.ToString())
                    .Append(" SpeedDecreaseEffect: (").Append(m_effect.ToString()).Append(')')
                    .ToString();
            }
        }

        public UnbreakableSpeedDecreaseSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override string ShortDescription
            => Spell.Value >= 99 ? "Target is rooted in place" : $"The target is slowed by {Spell.Value}%.";
    }
}
