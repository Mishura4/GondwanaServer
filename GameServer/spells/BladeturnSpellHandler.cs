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

using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("Bladeturn")]
    public class BladeturnSpellHandler : SpellHandler
    {
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
                    // Log an error or handle the fallback here
                    return "(Translation not found)";
                }
            }
            return string.Format(messageKey, args);
        }

        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            eChatType toLiving = (Spell.Pulse == 0) ? eChatType.CT_Spell : eChatType.CT_SpellPulse;
            eChatType toOther = (Spell.Pulse == 0) ? eChatType.CT_System : eChatType.CT_SpellPulse;

            // Handle translation for the effect owner
            if (effect.Owner is GamePlayer ownerPlayer)
            {
                MessageToLiving(effect.Owner, GetFormattedMessage(ownerPlayer, Spell.Message1), toLiving);
            }
            else
            {
                MessageToLiving(effect.Owner, LanguageMgr.GetTranslation("ServerLanguageKey", Spell.Message1), toLiving);
            }

            foreach (GamePlayer player in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            {
                if (!(effect.Owner == player))
                {
                    if (effect.Owner is GamePlayer targetPlayer)
                    {
                        player.MessageFromArea(effect.Owner, GetFormattedMessage(player, Spell.Message2, player.GetPersonalizedName(effect.Owner)), toOther, eChatLoc.CL_SystemWindow);
                    }
                    else
                    {
                        player.MessageFromArea(effect.Owner, LanguageMgr.GetTranslation("ServerLanguageKey", Spell.Message2), toOther, eChatLoc.CL_SystemWindow);
                    }
                }
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (!noMessages && Spell.Pulse == 0)
            {
                // Handle translation for the effect owner when it expires
                if (effect.Owner is GamePlayer ownerPlayer)
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
                        if (effect.Owner is GamePlayer targetPlayer)
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

        public override PlayerXEffect GetSavedEffect(GameSpellEffect e)
        {
            PlayerXEffect eff = new PlayerXEffect();
            eff.Var1 = Spell.ID;
            eff.Duration = e.RemainingTime;
            eff.IsHandler = true;
            eff.Var2 = (int)(Spell.Value * e.Effectiveness);
            eff.SpellLine = SpellLine.KeyName;
            return eff;
        }

        public override int OnRestoredEffectExpires(GameSpellEffect effect, int[] vars, bool noMessages)
        {
            if (!noMessages && Spell.Pulse == 0)
            {
                MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
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

        public BladeturnSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string ShortDescription => "Creates a 'bubble' that absorbs the damage of a single melee hit.";
    }
}
