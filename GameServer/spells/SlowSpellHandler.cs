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
using DOL.Language;

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
            // If slow duration is 0, just resist the spell
            return target.GetModified(eProperty.SpeedDecreaseDurationReduction) <= 0 ? 100 : 0;
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
                if (m_caster is GamePlayer player)
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client, "SpellHandler.DamageImmunity", player.GetPersonalizedName(target)), eChatType.CT_SpellResisted);
                else
                    MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.DamageImmunity", target.Name), eChatType.CT_SpellResisted);
                return;
            }
            if (target.EffectList.GetOfType<ChargeEffect>() != null)
            {
                if (m_caster is GamePlayer player)
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client, "SpellHandler.Target.TooFast", player.GetPersonalizedName(target)), eChatType.CT_SpellResisted);
                else
                    MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.Target.TooFast", target.Name), eChatType.CT_SpellResisted);
                return;
            }
            base.ApplyEffectOnTarget(target, effectiveness);
        }

        /// <inheritdoc />
        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            int duration = Spell.Duration;
            int modifiedSlowDuration = target.GetModified(eProperty.SpeedDecreaseDurationReduction);
            if (modifiedSlowDuration != 100)
            {
                if (modifiedSlowDuration <= 0) // Shouldn't happen because CalculateSpellResistChance but better safe than sending a duration of 0 (infinite)
                {
                    duration = 1;
                }
                else
                {
                    duration = (int)(duration * (modifiedSlowDuration / 100d) + 0.5d);
                }
            }
            return duration;
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
            if (effect.Owner.EffectList.GetOfType<ChargeEffect>() != null || effect.Owner.TempProperties.getProperty("Charging", false))
            {
                if (m_caster is GamePlayer player)
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client, "SpellHandler.SlowSpellHandler.TooFastForEffect", player.GetPersonalizedName(effect.Owner)), eChatType.CT_SpellResisted);
                else
                    MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.SlowSpellHandler.TooFastForEffect", effect.Owner.Name), eChatType.CT_SpellResisted);
                return;
            }
            effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect, 1.0 - Spell.Value * 0.01);
            SendUpdates(effect.Owner);

            GamePlayer ownerPlayer = effect.Owner as GamePlayer;

            // Use GetFormattedMessage for localized messages
            if (ownerPlayer != null)
            {
                MessageToLiving(effect.Owner, GetFormattedMessage(ownerPlayer, Spell.Message1), eChatType.CT_Spell);
            }
            else
            {
                MessageToLiving(effect.Owner, LanguageMgr.GetTranslation("ServerLanguageKey", Spell.Message1), eChatType.CT_Spell);
            }

            foreach (GamePlayer player in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            {
                if (!(effect.Owner == player))
                {
                    if (ownerPlayer != null)
                    {
                        player.MessageFromArea(effect.Owner, GetFormattedMessage(player, Spell.Message2, player.GetPersonalizedName(effect.Owner)), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                    }
                    else
                    {
                        player.MessageFromArea(effect.Owner, LanguageMgr.GetTranslation("ServerLanguageKey", Spell.Message2), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                    }
                }
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);
            GameTimer timer = (GameTimer)effect.Owner.TempProperties.getProperty<object>(effect, null);
            effect.Owner.TempProperties.removeProperty(effect);
            if (timer != null) timer.Stop();
            effect.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, effect);
            SendUpdates(effect.Owner);

            if (!noMessages)
            {
                GamePlayer ownerPlayer = effect.Owner as GamePlayer;

                if (ownerPlayer != null)
                {
                    MessageToLiving(effect.Owner, GetFormattedMessage(ownerPlayer, Spell.Message3), eChatType.CT_SpellExpires);
                }
                else
                {
                    MessageToLiving(effect.Owner, LanguageMgr.GetTranslation("ServerLanguageKey", Spell.Message3), eChatType.CT_SpellExpires);
                }

                foreach (GamePlayer player in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                {
                    if (!(effect.Owner == player))
                    {
                        if (ownerPlayer != null)
                        {
                            player.MessageFromArea(effect.Owner, GetFormattedMessage(player, Spell.Message4, player.GetPersonalizedName(effect.Owner)), eChatType.CT_SpellExpires, eChatLoc.CL_SystemWindow);
                        }
                        else
                        {
                            player.MessageFromArea(effect.Owner, LanguageMgr.GetTranslation("ServerLanguageKey", Spell.Message4), eChatType.CT_SpellExpires, eChatLoc.CL_SystemWindow);
                        }
                    }
                }
            }

            return 0;
        }

        private string GetFormattedMessage(GamePlayer player, string messageKey, params object[] args)
        {
            if (messageKey.StartsWith("Languages.DBSpells."))
            {
                string translationKey = messageKey;
                string translation;

                if (LanguageMgr.TryGetTranslation(out translation, player.Client.Account.Language, translationKey, args))
                {
                    return translation;
                }
                else
                {
                    return "(Translation not found)";
                }
            }
            return string.Format(messageKey, args);
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