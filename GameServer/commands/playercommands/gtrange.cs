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
using DOL.GS.Geometry;
using System.Numerics;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&gtrange",
        ePrivLevel.Player,
        "Commands.Players.Gtrange.Description",
        "Commands.Players.Gtrange.Usage")]
    public class GroundTargetRangeCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "gtrange"))
                return;

            if (client.Player.GroundTargetPosition != Position.Nowhere)
            {
                var range = (int)client.Player.Coordinate.DistanceTo(client.Player.GroundTargetPosition);
                client.Out.SendMessage(
                    LanguageMgr.GetTranslation(
                        client.Account.Language,
                        "Commands.Players.Gtrange.Range",
                        range
                    ),
                    eChatType.CT_System,
                    eChatLoc.CL_SystemWindow
                );
            }
            else
                client.Out.SendMessage(
                    LanguageMgr.GetTranslation(
                        client.Account.Language,
                        "Commands.Players.Gtrange.Missing.Groundtarget"
                    ),
                    eChatType.CT_System,
                    eChatLoc.CL_SystemWindow
                );
        }
    }
}