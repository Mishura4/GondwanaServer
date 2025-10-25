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
using DOL.GS.SkillHandler;
using DOL.Language;
using DOL.GS.Scripts;
using DOL.GS.Spells;

namespace DOL.GS.Commands
{
    [Cmd(
        "&afk",
        ePrivLevel.Player,
        "Commands.Players.Afk.Description",
        "Commands.Players.Afk.Usage")]
    public class AFKCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            var p = client?.Player;
            if (p == null) return;

            if (p.IsAfkActive() && args.Length == 1)
            {
                p.ClearAFK(showMessage: true);
                p.DisableSkill(SkillBase.GetAbility(Abilities.Vol), VolAbilityHandler.DISABLE_DURATION_PLAYER);
                return;
            }

            if (p.DuelTarget != null)
            {
                client!.Out.SendMessage(
                    LanguageMgr.GetTranslation(client, "Commands.Players.Afk.CannotWhileDuel"),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (p.InCombat)
            {
                client!.Out.SendMessage(
                    LanguageMgr.GetTranslation(client, "Commands.Players.Afk.CannotWhileCombat"),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (p.TempProperties.getProperty<object>(StealCommandHandlerBase.PLAYER_VOL_TIMER, null) != null)
            {
                client!.Out.SendMessage(
                    LanguageMgr.GetTranslation(client, "Commands.Players.Afk.CannotWhileStealing"),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (p.IsRiding)
            {
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client, "Commands.Players.Afk.CannotWhileRiding"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (p.IsMoving)
            {
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client, "Commands.Players.Afk.CannotWhileMoving"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (JailMgr.IsPrisoner(p))
            {
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client, "Commands.Players.Afk.CannotWhileJailed"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (p.IsStunned)
            {
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client, "Commands.Players.Afk.CannotWhileStunned"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (p.IsMezzed)
            {
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client, "Commands.Players.Afk.CannotWhileMezzed"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (p.IsDamned)
            {
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client, "Commands.Players.Afk.CannotWhileDamned"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (p.IsCrafting)
            {
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client, "Commands.Players.Afk.CannotWhileCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            var wsdSrc = SpellHandler.FindEffectOnTarget(p, "WarlockSpeedDecrease");
            if (wsdSrc != null)
            {
                int rm = wsdSrc.Spell?.ResurrectMana ?? 0;
                string appearance = LanguageMgr.GetWarlockMorphAppearance(p.Client.Account.Language, rm);
                p.Out.SendMessage(
                    LanguageMgr.GetTranslation(p.Client, "Commands.Players.Afk.CannotWhilekMorphed", appearance),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (!p.IsAfkDelayElapsed)
            {
                client!.Out.SendMessage(
                    LanguageMgr.GetTranslation(client, "Commands.Players.Afk.Wait"),
                    eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
                return;
            }

            p.InitAfkTimers();

            string msg = args.Length > 1
                ? string.Join(" ", args, 1, args.Length - 1)
                : "AFK";

            p.SetAFK(msg, showMessage: true);
        }
    }
}
