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
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Events;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Made by Dargon/Mannik
    /// 
    /// Thanks to PlanarChaosRvrTwo, and Batlas for their assistance for making it not interupt
    /// Special thanks to ontheDOL for actually solving my issues with interupt :)
    /// 
    /// Spell handler for slow spells. These spells have no immunity, and is not lost on hit.
    /// </summary>
    [SpellHandler("Slow")]
    public class SlowSpellHandler : SpellHandler
    {
        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }


        /// <summary>
        /// Apply the effect.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effectiveness"></param>
        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target.HasAbility(Abilities.CCImmunity) || target.HasAbility(Abilities.RootImmunity))
            {
                MessageToCaster(target.Name + " is immune to this effect!", eChatType.CT_SpellResisted);
                return;
            }
            if (target.EffectList.GetOfType<ChargeEffect>() != null)
            {
                MessageToCaster(target.Name + " is moving to fast for this spell to have any effect!", eChatType.CT_SpellResisted);
                return;
            }
            base.ApplyEffectOnTarget(target, effectiveness);

        }

        /// <summary>
        /// Sends updates on effect start/stop
        /// </summary>
        /// <param name="owner"></param>
        protected static void SendUpdates(GameLiving owner)
        {
            if (owner.IsMezzed || owner.IsStunned)
                return;

            GamePlayer player = owner as GamePlayer;
            if (player != null)
                player.Out.SendUpdateMaxSpeed();

            GameNPC npc = owner as GameNPC;
        }

        /// <summary>
        /// When an applied effect starts
        /// </summary>
        /// <param name="effect"></param>
        public override void OnEffectStart(GameSpellEffect effect)
        {
            // Cannot apply if the effect owner has a charging effect
            if (effect.Owner.EffectList.GetOfType<ChargeEffect>() != null || effect.Owner.TempProperties.getProperty("Charging", false))
            {
                MessageToCaster(effect.Owner.Name + " is moving too fast for this spell to have any effect!", eChatType.CT_SpellResisted);
                return;
            }
            effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect, 1.0 - Spell.Value * 0.01);
            SendUpdates(effect.Owner);
            MessageToLiving(effect.Owner, Spell.Message1, eChatType.CT_Spell);
            Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, true)), eChatType.CT_Spell, effect.Owner);


        }

        /// <summary>
        /// When an applied effect expires.
        /// </summary>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);
            GameTimer timer = (GameTimer)effect.Owner.TempProperties.getProperty<object>(effect, null);
            effect.Owner.TempProperties.removeProperty(effect);
            if (timer != null) timer.Stop();
            effect.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, effect);
            SendUpdates(effect.Owner);
            MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
            Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, true)), eChatType.CT_SpellExpires, effect.Owner);

            return 0;
        }

        // constructor
        public SlowSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
        }
        public override string ShortDescription
            => Spell.Value >= 99 ? "Target is rooted in place" : $"The target is slowed by {Spell.Value}%. There's no immunity against this spell and it cannot be interrupted by hits.";
    }
}