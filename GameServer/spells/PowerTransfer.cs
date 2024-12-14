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
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("PowerTransfer")]
    class PowerTransfer : SpellHandler
    {
        public override bool CheckBeginCast(GameLiving selectedTarget, bool quiet)
        {
            GamePlayer owner = Owner();
            if (owner == null || selectedTarget == null)
                return false;

            if (selectedTarget == Caster || selectedTarget == owner)
            {
                owner.Out.SendMessage(LanguageMgr.GetTranslation(owner.Client, "SpellHandler.PowerTransfer.CannotTransferToSelf"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return false;
            }

            return base.CheckBeginCast(selectedTarget, quiet);
        }

        public override bool OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target == null) return false;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return false;

            // Calculate the amount of power to transfer from the owner.
            // TODO: Effectiveness plays a part here.

            GamePlayer owner = Owner();
            if (owner == null)
                return false;

            int powerTransfer = (int)Math.Min(Spell.Value, owner.Mana);
            int powerDrained = -owner.ChangeMana(owner, GameLiving.eManaChangeType.Spell, -powerTransfer);

            if (powerDrained <= 0)
                return true;

            int powerHealed = target.ChangeMana(owner, GameLiving.eManaChangeType.Spell, powerDrained);

            if (powerHealed <= 0)
            {
                SendEffectAnimation(target, 0, false, 0);
                owner.Out.SendMessage(LanguageMgr.GetTranslation(owner.Client, "SpellHandler.PowerHeal.FullPowerOther", owner.GetPersonalizedName(target)), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
            }
            else
            {
                SendEffectAnimation(target, 0, false, 1);
                owner.Out.SendMessage(LanguageMgr.GetTranslation(owner.Client, "SpellHandler.PowerTransfer.TransferSuccess", powerHealed, owner.GetPersonalizedName(target)), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);

                if (target is GamePlayer playerTarget)
                    playerTarget.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "SpellHandler.PowerTransfer.PowerReceived", playerTarget.GetPersonalizedName(owner), powerHealed), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
            }
            return true;
        }

        protected virtual GamePlayer Owner()
        {
            if (Caster is GamePlayer)
                return Caster as GamePlayer;

            return null;
        }

        public PowerTransfer(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override string ShortDescription
        {
            get
            {
                string language = Properties.SERV_LANGUAGE;
                return LanguageMgr.GetTranslation(language, "SpellDescription.PowerTransfer.MainDescription", Spell.Value);
            }
        }
    }
}
