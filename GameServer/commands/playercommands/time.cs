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
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&time",
        ePrivLevel.Player,
        "Commands.Players.Time.Description",
        "Commands.Players.Time.Usage")]
    public class TimeCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "time", 1000))
                return;

            if (client.Account.PrivLevel == (int)ePrivLevel.Admin) // admins only
            {
                try
                {
                    if (args.Length == 3)
                    {
                        uint speed = 0;
                        uint time = 0;

                        speed = Convert.ToUInt32(args[1]);
                        time = Convert.ToUInt32(args[2]);

                        WorldMgr.StartDay(speed, time / 1000.0);
                        return;
                    }
                    else throw new Exception();
                }
                catch
                {
                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Time.Usage01.Admin"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Time.Usage02.Admin"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Time.Usage03.Admin"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
            }

            if (client.Player != null)
            {
                uint cTime = WorldMgr.GetCurrentGameTime(client.Player) / 1000;

                uint hour = cTime / 60 / 60;
                uint minute = cTime / 60 % 60;
                uint seconds = cTime % 60;
                bool pm = false;

                if (hour == 0)
                {
                    hour = 12;
                }
                else if (hour == 12)
                {
                    pm = true;
                }
                else if (hour > 12)
                {
                    hour -= 12;
                    pm = true;
                }

                client.Out.SendMessage(
                    LanguageMgr.GetTranslation(
                        client.Account.Language,
                        "Commands.Players.Time.Print",
                        hour.ToString(), minute.ToString("00"), seconds.ToString("00"), (pm ? " pm" : "")),
                                       eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
    }
}