﻿/* 
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

using DOL.Language;
using DOL.GS.Appeal;
using DOL.GS.ServerProperties;

namespace DOL.GS.Commands
{
    //The appeal command is really just a tool to redirect the players concerns to the proper command.
    //most of it's functionality is built into the client.
    [CmdAttribute(
        "&appeal",
        ePrivLevel.Player,
        "Commands.Players.Appeal.Description",
        "Commands.Players.Appeal.Usage")]
    public class AppealCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "appeal"))
                return;

            if (ServerProperties.Properties.DISABLE_APPEALSYSTEM)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.SystemDisabled"));
                return;
            }

            if (client.Player.IsMuted)
            {
                return;
            }

            //Help display
            if (args.Length == 1)
            {
                DisplaySyntax(client);
                if (client.Account.PrivLevel > (uint)ePrivLevel.Player)
                {
                    AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.UseGMappeal"));
                }
            }

            //Support for EU Clients

            if (args.Length == 2 && args[1].ToLower() == "cancel")
            {
                CheckAppealCommandHandler cch = new CheckAppealCommandHandler();
                cch.OnCommand(client, args);
                return;
            }

            if (args.Length > 1)
            {
                bool HasPendingAppeal = client.Player.TempProperties.getProperty<bool>("HasPendingAppeal");
                if (HasPendingAppeal)
                {
                    AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.AlreadyActiveAppeal", client.Player.Name));
                    return;
                }
                if (args.Length < 5)
                {
                    AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.NeedMoreDetail"));
                    return;
                }
                int severity = 0;
                switch (args[1].ToLower())
                {
                    case "harassment":
                    case "harcèlement":
                        {
                            severity = (int)AppealMgr.eSeverity.High;
                            args[1] = "";
                            break;
                        }
                    case "naming":
                    case "nom":
                        {
                            severity = (int)AppealMgr.eSeverity.Low;
                            args[1] = "";
                            break;
                        }
                    case "other":
                    case "conduct":
                    case "autre":
                    case "conduite":
                        {
                            severity = (int)AppealMgr.eSeverity.Medium;
                            args[1] = "";
                            break;
                        }
                    case "stuck":
                    case "emergency":
                    case "bloqué":
                    case "urgence":
                        {
                            severity = (int)AppealMgr.eSeverity.Critical;
                            args[1] = "";
                            break;
                        }
                    default:
                        {
                            severity = (int)AppealMgr.eSeverity.Medium;
                            break;
                        }

                }
                string message = string.Join(" ", args, 1, args.Length - 1);
                GamePlayer p = client.Player as GamePlayer;
                AppealMgr.CreateAppeal(p, severity, "Open", message);
                return;
            }
            return;
        }
    }

    #region reportbug
    //handles /reportbug command that is issued from the client /appeal function.
    [CmdAttribute(
    "&reportbug",
    ePrivLevel.Player, "Commands.Players.Appeal.UseAppeal")]
    public class ReportBugCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (ServerProperties.Properties.DISABLE_APPEALSYSTEM)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.SystemDisabled"));
                return;
            }

            if (args.Length < 5)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.NeedMoreDetail"));
                return;
            }

            //send over the info to the /report command
            args[1] = "";
            //strip these words if they are the first word in the bugreport text
            switch (args[2].ToLower())
            {
                case "harassment":
                case "naming":
                case "other":
                case "conduct":
                case "stuck":
                case "emergency":
                    {
                        args[2] = "";
                        break;
                    }
            }
            ReportCommandHandler report = new ReportCommandHandler();
            report.OnCommand(client, args);
            AppealMgr.MessageToAllStaff(client.Player.Name + " submitted a bug report.");
            return;
        }
    }
    #endregion reportbug
    #region reportharass
    //handles /reportharass command that is issued from the client /appeal function.
    [CmdAttribute(
    "&reportharass",
    ePrivLevel.Player, "Commands.Players.Appeal.UseAppeal")]
    public class ReportHarassCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (ServerProperties.Properties.DISABLE_APPEALSYSTEM)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.SystemDisabled"));
                return;
            }
            bool HasPendingAppeal = client.Player.TempProperties.getProperty<bool>("HasPendingAppeal");
            if (HasPendingAppeal)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.AlreadyActiveAppeal", client.Player.Name));
                return;
            }
            if (args.Length < 7)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.NeedMoreDetail"));
                return;
            }
            //strip these words if they are the first word in the appeal text
            switch (args[1].ToLower())
            {
                case "harassment":
                case "naming":
                case "other":
                case "conduct":
                case "stuck":
                case "emergency":
                    {
                        args[1] = "";
                        break;
                    }
            }
            string message = string.Join(" ", args, 1, args.Length - 1);
            GamePlayer p = client.Player as GamePlayer;
            AppealMgr.CreateAppeal(p, (int)AppealMgr.eSeverity.High, "Open", message);
            return;
        }
    }
    #endregion reportharass
    #region reporttos
    //handles /reporttos command that is issued from the client /appeal function.
    [CmdAttribute(
    "&reporttos",
    ePrivLevel.Player, "Commands.Players.Appeal.UseAppeal")]
    public class ReportTosCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (ServerProperties.Properties.DISABLE_APPEALSYSTEM)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.SystemDisabled"));
                return;
            }
            bool HasPendingAppeal = client.Player.TempProperties.getProperty<bool>("HasPendingAppeal");
            if (HasPendingAppeal)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.AlreadyActiveAppeal", client.Player.Name));
                return;
            }
            if (args.Length < 7)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.NeedMoreDetail"));
                return;
            }
            switch (args[1])
            {
                case "NAME":
                    {
                        //strip these words if they are the first word in the appeal text
                        switch (args[2].ToLower())
                        {
                            case "harassment":
                            case "naming":
                            case "other":
                            case "conduct":
                            case "stuck":
                            case "emergency":
                                {
                                    args[2] = "";
                                    break;
                                }
                        }
                        string message = string.Join(" ", args, 2, args.Length - 2);
                        GamePlayer p = client.Player as GamePlayer;
                        AppealMgr.CreateAppeal(p, (int)AppealMgr.eSeverity.Low, "Open", message);
                        break;
                    }
                case "TOS":
                    {
                        //strip these words if they are the first word in the appeal text
                        switch (args[2].ToLower())
                        {
                            case "harassment":
                            case "naming":
                            case "other":
                            case "conduct":
                            case "stuck":
                            case "emergency":
                                {
                                    args[2] = "";
                                    break;
                                }
                        }
                        string message = string.Join(" ", args, 2, args.Length - 2);
                        GamePlayer p = client.Player as GamePlayer;
                        AppealMgr.CreateAppeal(p, (int)AppealMgr.eSeverity.Medium, "Open", message);
                        break;
                    }
            }
            return;
        }
    }
    #endregion reporttos
    #region reportstuck
    //handles /reportharass command that is issued from the client /appeal function.
    [CmdAttribute(
    "&reportstuck",
    ePrivLevel.Player, "Commands.Players.Appeal.UseAppeal")]
    public class ReportStuckCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (ServerProperties.Properties.DISABLE_APPEALSYSTEM)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.SystemDisabled"));
                return;
            }
            bool HasPendingAppeal = client.Player.TempProperties.getProperty<bool>("HasPendingAppeal");
            if (HasPendingAppeal)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.AlreadyActiveAppeal", client.Player.Name));
                return;
            }
            if (args.Length < 5)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.NeedMoreDetail"));
                return;
            }
            //strip these words if they are the first word in the appeal text
            switch (args[1].ToLower())
            {
                case "harassment":
                case "naming":
                case "other":
                case "conduct":
                case "stuck":
                case "emergency":
                    {
                        args[1] = "";
                        break;
                    }
            }
            string message = string.Join(" ", args, 1, args.Length - 1);
            GamePlayer p = client.Player as GamePlayer;
            AppealMgr.CreateAppeal(p, (int)AppealMgr.eSeverity.Critical, "Open", message);
            return;
        }
    }
    #endregion reportstuck
    #region emergency
    //handles /appea command that is issued from the client /appeal function (emergency appeal).
    [CmdAttribute(
    "&appea",
    ePrivLevel.Player, "Commands.Players.Appeal.UseAppeal")]
    public class EmergencyAppealCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (ServerProperties.Properties.DISABLE_APPEALSYSTEM)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.SystemDisabled"));
                return;
            }
            bool HasPendingAppeal = client.Player.TempProperties.getProperty<bool>("HasPendingAppeal");
            if (HasPendingAppeal)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.AlreadyActiveAppeal", client.Player.Name));
                return;
            }
            if (args.Length < 5)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Appeal.NeedMoreDetail"));
                return;
            }
            //strip these words if they are the first word in the appeal text
            switch (args[1].ToLower())
            {
                case "harassment":
                case "naming":
                case "other":
                case "conduct":
                case "stuck":
                case "emergency":
                    {
                        args[1] = "";
                        break;
                    }
            }
            string message = string.Join(" ", args, 1, args.Length - 1);
            GamePlayer p = client.Player as GamePlayer;
            AppealMgr.CreateAppeal(p, (int)AppealMgr.eSeverity.Critical, "Open", message);
            return;
        }
    }
    #endregion emergency
}