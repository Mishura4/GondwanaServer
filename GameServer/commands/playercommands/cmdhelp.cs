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
                DisplayMessage(client, " ");
                DisplayCategory(client, ePrivLevel.Admin, "<--------------- ADMIN Commands --------------->", ePrivLevel.Admin);
                DisplayMessage(client, " ");
                DisplayCategory(client, ePrivLevel.GM, "<----------------- GM Commands ----------------->", ePrivLevel.GM);
                DisplayMessage(client, " ");
                DisplayCategory(client, ePrivLevel.Player, "<--------------- Player Commands --------------->", ePrivLevel.Player);
            }
            else if (privilegeLevel == ePrivLevel.GM)
            {
                DisplayMessage(client, " ");
                DisplayCategory(client, ePrivLevel.GM, "<----------------- GM Commands ----------------->", ePrivLevel.GM);
                DisplayMessage(client, " ");
                DisplayCategory(client, ePrivLevel.Player, "<--------------- Player Commands --------------->", ePrivLevel.Player);
            }
            else if (privilegeLevel == ePrivLevel.Player)
            {
                DisplayMessage(client, " ");
                DisplayCategory(client, ePrivLevel.Player, "<--------------- Player Commands --------------->", ePrivLevel.Player);
            }
        }

        private void DisplayCategory(GameClient client, ePrivLevel categoryLevel, string categoryTitle, ePrivLevel plvl)
        {
            string[] commandList = GetCommandList(client, plvl);
            if (commandList.Length == 0) return;

            DisplayMessage(client, categoryTitle);
            foreach (string commandLine in commandList)
            {
                string cmdName;
                string desc;
                int dashIndex = commandLine.IndexOf(" - ");

                if (dashIndex > 0)
                {
                    cmdName = commandLine.Substring(0, dashIndex).Trim();
                    desc = commandLine.Substring(dashIndex + 3).Trim();
                }
                else
                {
                    cmdName = commandLine.Trim();
                    desc = string.Empty;
                }

                if (!string.IsNullOrEmpty(desc))
                {
                    string translatedDesc = LanguageMgr.GetTranslation(client.Account.Language, desc);
                    DisplayMessage(client, cmdName + " - " + translatedDesc);
                }
                else
                {
                    DisplayMessage(client, cmdName);
                }
            }
        }

        private static Dictionary<ePrivLevel, Dictionary<string, String[]>> m_commandLists = new();
        
        private static object m_syncObject = new object();

        private String[] GetCommandList(GameClient client, ePrivLevel privilegeLevel)
        {
            string language = client?.Account?.Language ?? Properties.SERV_LANGUAGE;
            lock (m_syncObject)
            {
                Dictionary<string, string[]> langDict;
                if (!m_commandLists.TryGetValue(privilegeLevel, out langDict))
                {
                    langDict = new Dictionary<string, string[]>();
                    m_commandLists[privilegeLevel] = langDict;
                }
                if (!langDict.TryGetValue(language, out string[] commandList))
                {
                    commandList = ScriptMgr.GetCommandList(privilegeLevel, true, language);
                    Array.Sort(commandList);
                    langDict[language] = commandList;
                }
                return commandList;
            }
        }
    }
}