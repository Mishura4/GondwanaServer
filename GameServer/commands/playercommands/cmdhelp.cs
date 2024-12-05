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
using System.Collections.Generic;

namespace DOL.GS.Commands
{
    [CmdAttribute("&cmdhelp",
        ePrivLevel.Player,
        "Commands.Players.Cmdhelp.Description",
        "Commands.Players.Cmdhelp.Usage",
        "Commands.Players.Cmdhelp.Usage.Plvl",
        "Commands.Players.Cmdhelp.Usage.Cmd")]
    public class CmdHelpCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "cmdhelp"))
                return;

            ePrivLevel privilegeLevel = (ePrivLevel)client.Account.PrivLevel;

            if (args.Length > 1)
            {
                if (uint.TryParse(args[1], out uint requestedLevel))
                {
                    privilegeLevel = (ePrivLevel)requestedLevel;
                }
            }

            if (privilegeLevel >= ePrivLevel.Admin)
            {
                DisplayCategory(client, ePrivLevel.Admin, "----- ADMIN Commands ----", ePrivLevel.Admin);
                DisplaySeparator(client);
                DisplayCategory(client, ePrivLevel.GM, "----- GM Commands ----", ePrivLevel.GM);
                DisplaySeparator(client);
                DisplayCategory(client, ePrivLevel.Player, "----- Player Commands ----", ePrivLevel.Player);
            }
            else if (privilegeLevel == ePrivLevel.GM)
            {
                DisplayCategory(client, ePrivLevel.GM, "----- GM Commands ----", ePrivLevel.GM);
                DisplaySeparator(client);
                DisplayCategory(client, ePrivLevel.Player, "----- Player Commands ----", ePrivLevel.Player);
            }
            else if (privilegeLevel == ePrivLevel.Player)
            {
                DisplayCategory(client, ePrivLevel.Player, "----- Player Commands ----", ePrivLevel.Player);
            }
        }

        private void DisplayCategory(GameClient client, ePrivLevel categoryLevel, string categoryTitle, ePrivLevel exactLevel)
        {
            String[] commandList = GetCommandListForExactLevel(exactLevel);
            if (commandList.Length == 0) return;

            DisplayMessage(client, categoryTitle);
            foreach (String command in commandList)
            {
                DisplayMessage(client, command);
            }
        }

        private void DisplaySeparator(GameClient client)
        {
            DisplayMessage(client, " ");
        }

        private static IDictionary<ePrivLevel, String[]> m_commandLists = new Dictionary<ePrivLevel, String[]>();
        private static object m_syncObject = new object();

        private String[] GetCommandListForExactLevel(ePrivLevel privilegeLevel)
        {
            lock (m_syncObject)
            {
                if (!m_commandLists.ContainsKey(privilegeLevel))
                {
                    String[] commandList = ScriptMgr.GetCommandListForExactLevel(privilegeLevel, true);
                    Array.Sort(commandList);
                    m_commandLists[privilegeLevel] = commandList;
                }

                return m_commandLists[privilegeLevel];
            }
        }
    }
}