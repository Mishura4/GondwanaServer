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
using System.Collections;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&target",
        ePrivLevel.Player,
        "Commands.Players.Target.Description",
        "Commands.Players.Target.Usage")]
    public class TargetCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "target"))
                return;

            GamePlayer targetPlayer = null;
            if (args.Length == 2)
            {
                int result = 0;
                GameClient targetClient = WorldMgr.GuessClientByPlayerNameAndRealm(args[1], 0, true, out result);
                if (targetClient != null)
                {
                    targetPlayer = targetClient.Player;

                    if (!client.Player.IsWithinRadius(targetPlayer, WorldMgr.YELL_DISTANCE) || targetPlayer.IsStealthed || GameServer.ServerRules.IsAllowedToAttack(client.Player, targetPlayer, true))
                    {
                        client.Out.SendMessage(
                            LanguageMgr.GetTranslation(
                                client.Account.Language,
                                "Commands.Players.Target.NotFound",
                                args[1]),
                            eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    client.Out.SendChangeTarget(targetPlayer);
                    client.Out.SendMessage(
                        LanguageMgr.GetTranslation(
                            client.Account.Language,
                            "Commands.Players.Target.Found",
                            client.Player.GetPersonalizedName(targetPlayer)),
                        eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }
                if (client.Account.PrivLevel > 1)
                {
                    IEnumerator en = client.Player.GetNPCsInRadius(800).GetEnumerator();
                    while (en.MoveNext())
                    {
                        if (((GameObject)en.Current).Name == args[1])
                        {
                            client.Out.SendChangeTarget((GameObject)en.Current);
                            client.Out.SendMessage(
                                LanguageMgr.GetTranslation(
                                    client.Account.Language,
                                    "Commands.Players.Target.GM.Found",
                                    ((GameObject)en.Current).GetName(0, true)),
                                eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                    }
                }

                client.Out.SendMessage(
                    LanguageMgr.GetTranslation(
                        client.Account.Language,
                        "Commands.Players.Target.NotFound",
                        args[1]),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (client.Account.PrivLevel > 1)
            {
                client.Out.SendMessage(
                    LanguageMgr.GetTranslation(
                        client.Account.Language,
                        "Commands.Players.Target.GM.Help",
                        args[1]),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            else
            {
                client.Out.SendMessage(
                    LanguageMgr.GetTranslation(
                        client.Account.Language,
                        "Commands.Players.Target.Usage",
                        args[1]),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
    }
}