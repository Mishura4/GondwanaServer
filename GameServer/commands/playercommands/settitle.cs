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
using System.Linq;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using DOL.GS.Commands;
using DOL.Language;

namespace DOL.GS.Commands
{
    [Cmd(
         "&settitle",
         ePrivLevel.Player,
         "Commands.Players.Settitle.Description",
         "Commands.Players.Settitle.Usage")]
    public class SetTitleCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "settitle"))
                return;

            int index = -1;
            if (args.Length < 2 || !int.TryParse(args[1], out index))
            {
                DisplaySyntax(client);
                return;
            }
            
            if (client.Player.CurrentTitle?.IsForced(client.Player) == true)
            {
                client.SendTranslation("Commands.Players.Settitle.Cannot", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            var titles = client.Player.Titles.ToArray();
            if (index < 0 || index >= titles.Length)
                client.Player.CurrentTitle = PlayerTitleMgr.ClearTitle;
            else
                client.Player.CurrentTitle = (IPlayerTitle)titles[index];
        }
    }
}
