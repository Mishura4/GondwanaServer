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
using Discord;
using System;
using System.Collections;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.Language;
using System.Numerics;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("PowerLock")]
    public class PowerLock : SpellHandler
    {
        internal class PowerLockEffect : GameSpellEffect
        {
            private PowerLockEffect m_linkedEffect;
            
            /// <inheritdoc />
            public PowerLockEffect(ISpellHandler handler, int duration, int pulseFreq) : base(handler, duration, pulseFreq)
            {
            }

            /// <inheritdoc />
            public PowerLockEffect(ISpellHandler handler, int duration, int pulseFreq, double effectiveness) : base(handler, duration, pulseFreq, effectiveness)
            {
            }

            public void Link(PowerLockEffect other)
            {
                other.m_linkedEffect = this;
                this.m_linkedEffect = other;
            }

            /// <inheritdoc />
            public override void Cancel(bool playerCanceled, bool force = false)
            {
                m_linkedEffect?.RealCancel(playerCanceled, force);
                RealCancel(playerCanceled, force);
            }

            private void RealCancel(bool playerCancelled, bool force)
            {
                base.Cancel(playerCancelled, force);
            }
        }

        /// <inheritdoc />
        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            int freq = Spell != null ? Spell.Frequency : 0;
            var duration = CalculateEffectDuration(target, effectiveness);
            return new PowerLockEffect(this, duration, freq, effectiveness);
        }

        private int CalculateBaseMana(GameLiving living)
        {
            // Logic taken from MaxManaCalculator.cs
            
            if (living is not GamePlayer player)
                return Math.Max(5, (living.Level * 5) + (living.GetModified(eProperty.Intelligence) - 25));
            
            eStat manaStat = player.CharacterClass.ManaStat;

            if (player.CharacterClass.ManaStat == eStat.UNDEFINED)
            {
                //Special handling for Vampiirs:
                /* There is no stat that affects the Vampiir's power pool or the damage done by its power based spells.
                 * The Vampiir is not a focus based class like, say, an Enchanter.
                 * The Vampiir is a lot more cut and dried than the typical casting class.
                 * EDIT, 12/13/04 - I was told today that this answer is not entirely accurate.
                 * While there is no stat that affects the damage dealt (in the way that intelligence or piety affects how much damage a more traditional caster can do),
                 * the Vampiir's power pool capacity is intended to be increased as the Vampiir's strength increases.
                 *
                 * This means that strength ONLY affects a Vampiir's mana pool
                 */
                if (player.CharacterClass.ID == (int)eCharacterClass.Vampiir)
                {
                    manaStat = eStat.STR;
                }
                else if (player.Champion && player.ChampionLevel > 0)
                {
                    return player.CalculateMaxMana(player.Level, 0);
                }
                else
                {
                    return 0;
                }
            }
            return player.CalculateMaxMana(player.Level, player.GetBaseStat(manaStat));
        }

        /// <inheritdoc />
        public override void OnEffectPulse(GameSpellEffect effect)
        {
            base.OnEffectPulse(effect);

            if (!effect.Owner.IsAlive || !Caster.IsAlive)
            {
                effect.Cancel(false);
                return;
            }

            if (effect.Owner == Caster)
                return;

            int victimMana = CalculateBaseMana(effect.Owner);
            int maxDrain = (int)Math.Ceiling(victimMana * (m_spell.LifeDrainReturn / 100.0));
            int actuallyDrained = -effect.Owner.ChangeMana(Caster, GameLiving.eManaChangeType.Spell, -maxDrain);

            if (actuallyDrained == 0)
                return;
            
            int restored = Caster.ChangeMana(Caster, GameLiving.eManaChangeType.Spell, actuallyDrained);
            int overflow = actuallyDrained - restored;
            if (Caster is GamePlayer casterPlayer)
            {
                if (overflow > 0)
                {
                    casterPlayer.SendTranslatedMessage(
                        "SpellHandler.PowerLock.StealOverflow", eChatType.CT_Spell, eChatLoc.CL_SystemWindow,
                        casterPlayer.GetPersonalizedName(effect.Owner),
                        restored,
                        overflow
                    );
                }
                else
                {
                    casterPlayer.SendTranslatedMessage(
                        "SpellHandler.PowerLock.StealSuccess", eChatType.CT_Spell, eChatLoc.CL_SystemWindow,
                        casterPlayer.GetPersonalizedName(effect.Owner),
                        actuallyDrained
                    );
                }
            }
            if (effect.Owner is GamePlayer victimPlayer)
            {
                victimPlayer.SendTranslatedMessage(
                    "SpellHandler.PowerLock.SpellHit", eChatType.CT_Spell, eChatLoc.CL_SystemWindow,
                    victimPlayer.GetPersonalizedName(effect.Owner),
                    actuallyDrained
                );
            }
        }

        /// <inheritdoc />
        public override void OnEffectStart(GameSpellEffect effect)
        {
            if (effect.Owner != Caster)
            {
                SendHitAnimation(effect.Owner, 0, false, 1);

                var masterEffect = new PowerLockEffect(this, effect.Duration, effect.PulseFreq, effect.Effectiveness);
                masterEffect.Link(effect as PowerLockEffect);
                Caster.EffectList.BeginChanges();
                try
                {
                    masterEffect.Start(Caster);
                }
                finally
                {
                    Caster.EffectList.CommitChanges();
                }
            }
            if (effect.Owner is GamePlayer player)
            {
                if (player.EffectList.GetOfType<ChargeEffect>() == null)
                {
                    effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect, 0);
                    player.Client.Out.SendUpdateMaxSpeed();
                }
            }
            else
            {
                effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect, 0);
            }
            effect.Owner.StopAttack();
            effect.Owner.StopCurrentSpellcast();
            effect.Owner.DisarmedCount++;
            effect.Owner.SilencedCount++;
        }

        /// <inheritdoc />
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);

            effect.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, effect);
            if (effect.Owner is GamePlayer player)
                player.Client.Out.SendUpdateMaxSpeed();
            effect.Owner.DisarmedCount--;
            effect.Owner.SilencedCount--;
            return 0;
        }

        /// <summary>
        /// Create a new handler for the necro power lock spell.
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="spell"></param>
        /// <param name="line"></param>
        public PowerLock(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        /// <inheritdoc />
        public override string GetDelveDescription(GameClient delveClient)
        {
            string description1 = LanguageMgr.GetTranslation(delveClient, "SpellDescription.PowerLock.MainDescription1", Spell.LifeDrainReturn, Spell.Frequency / 1000.0, Spell.Duration / 1000);
            string description2 = LanguageMgr.GetTranslation(delveClient, "SpellDescription.PowerLock.MainDescription2");

            if (!Spell.AllowBolt)
            {
                string description3 = LanguageMgr.GetTranslation(delveClient, "SpellDescription.PowerLock.MainDescription3");
                return description1 + "\n\n" + description2 + "\n\n" + description3;
            }
            else
            {
                return description1 + "\n\n" + description2;
            }
        }
    }
}
