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
 *///made by DeMAN
using System;
using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.Database;
using DOL.GS.Effects;
using DOL.Events;
using log4net;
using DOL.Language;


namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("WaterBreathing")]
    public class WaterBreathingSpellHandler : SpellHandler
    {
        public WaterBreathingSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        /// <summary>
        /// Calculates the effect duration in milliseconds
        /// </summary>
        /// <param name="target">The effect target</param>
        /// <param name="effectiveness">The effect effectiveness</param>
        /// <returns>The effect duration in milliseconds</returns>
        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            double duration = Spell.Duration;
            duration *= (1.0 + m_caster.GetModified(eProperty.SpellDuration) * 0.01);
            return (int)duration;
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

        public override void OnEffectStart(GameSpellEffect effect)
        {
            GamePlayer player = effect.Owner as GamePlayer;

            if (player != null)
            {
                player.CanBreathUnderWater = true;
                player.BaseBuffBonusCategory[(int)eProperty.WaterSpeed] += (int)Spell.Value;
                player.Out.SendUpdateMaxSpeed();
            }

            eChatType toLiving = (Spell.Pulse == 0) ? eChatType.CT_Spell : eChatType.CT_SpellPulse;
            eChatType toOther = (Spell.Pulse == 0) ? eChatType.CT_System : eChatType.CT_SpellPulse;

            if (!string.IsNullOrEmpty(Spell.Message2))
            {
                foreach (GamePlayer player1 in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                {
                    if (!(effect.Owner == player1))
                    {
                        player1.MessageFromArea(effect.Owner, GetFormattedMessage(player1, Spell.Message2, player1.GetPersonalizedName(effect.Owner)), toOther, eChatLoc.CL_SystemWindow);
                    }
                }
            }

            GamePlayer ownerPlayer = effect.Owner as GamePlayer;
            if (ownerPlayer != null)
            {
                MessageToLiving(effect.Owner, GetFormattedMessage(ownerPlayer, Spell.Message1 == "" ? "SpellHandler.WaterBreathing.StartEffect" : Spell.Message1), toLiving);
            }
            else
            {
                MessageToLiving(effect.Owner, LanguageMgr.GetTranslation("ServerLanguageKey", Spell.Message1 == "" ? "SpellHandler.WaterBreathing.StartEffect" : Spell.Message1), toLiving);
            }

            base.OnEffectStart(effect);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GamePlayer player = effect.Owner as GamePlayer;

            if (player != null)
            {
                //Check for Mythirian of Ektaktos on effect expiration to prevent unneccessary removal of Water Breathing Effect
                InventoryItem item = player.Inventory.GetItem((eInventorySlot)37);
                if (item == null || !item.Name.ToLower().Contains("ektaktos"))
                {
                    player.CanBreathUnderWater = false;
                }
                player.BaseBuffBonusCategory[(int)eProperty.WaterSpeed] -= (int)Spell.Value;
                player.Out.SendUpdateMaxSpeed();
                if (player.IsDiving & player.CanBreathUnderWater == false)
                    MessageToLiving(effect.Owner, LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.WaterBreathing.EndEffect"), eChatType.CT_SpellExpires);
            }
            return base.OnEffectExpires(effect, noMessages);
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            int recastSeconds = Spell.RecastDelay / 1000;
            string mainDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.WaterBreathing.MainDescription", Spell.Value);

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
}
