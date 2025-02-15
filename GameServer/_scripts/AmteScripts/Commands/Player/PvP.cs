using System;
using System.Linq;
using DOL.Language;
using DOL.GS.Commands;
using AmteScripts.Managers;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    /// <summary>
    /// The new /pvp command for GMs or players to manage or view PvP state
    /// </summary>
    [Cmd(
        "&pvp",
        ePrivLevel.Player, // or ePrivLevel.Player if you want everyone to see subcommands like "info"
        "Commands.GM.PvP.Description",
        "Commands.GM.PvP.Usage.Open",
        "Commands.GM.PvP.Usage.Close",
        "Commands.GM.PvP.Usage.Unforce",
        "Commands.GM.PvP.Usage.Status",
        "Commands.GM.PvP.Usage.Refresh",
        "Commands.GM.PvP.Usage.Info",
        "Commands.GM.PvP.Usage.Reset")]
    public class PvpCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length <= 1)
            {
                DisplayCmd(client);
                return;
            }

            string subcmd = args[1].ToLower();

            if (client.Account.PrivLevel == 1 && subcmd != "info")
            {
                DisplayCmd(client);
                return;
            }

            ushort region = 0;

            switch (subcmd)
            {
                case "open":
                    {
                        if (args.Length >= 3 && !ushort.TryParse(args[2], out region))
                        {
                            DisplayCmd(client);
                            return;
                        }

                        bool success = PvpManager.Instance.Open(region, true);
                        if (success)
                        {
                            Region reg = WorldMgr.GetRegion(PvpManager.Instance.Region);
                            string regionDesc = (reg != null ? reg.Description : "Unknown Region");

                            string zoneNames = "no zones";
                            var zoneList = reg?.Zones?.ToArray() ?? Array.Empty<Zone>();
                            if (zoneList.Length > 0)
                            {
                                zoneNames = zoneList[0].Description;
                            }

                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.PvPOpened", PvpManager.Instance.Region, zoneNames));
                        }
                        else
                        {
                            Region reg = WorldMgr.GetRegion(PvpManager.Instance.Region);
                            string regionDesc = (reg != null ? reg.Description : "Unknown Region");

                            string zoneNames = "no zones";
                            var zoneList = reg?.Zones?.ToArray() ?? Array.Empty<Zone>();
                            if (zoneList.Length > 0)
                            {
                                zoneNames = zoneList[0].Description;
                            }

                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.PvPNotOpened", PvpManager.Instance.Region, zoneNames));
                        }
                        break;
                    }

                case "close":
                    {
                        bool success = PvpManager.Instance.Close();
                        if (success)
                        {
                            DisplayMessage(client,
                                LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.PvPClosed"));
                        }
                        else
                        {
                            DisplayMessage(client,
                                LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.PvPNotClosed"));
                        }
                        break;
                    }

                case "unforce":
                    {
                        if (!PvpManager.Instance.IsOpen)
                        {
                            DisplayMessage(client,
                                LanguageMgr.GetTranslation(client.Account.Language,
                                    "RvRManager.PvPUnforceNotPossible"));
                            break;
                        }
                        // We do an "unforce" by calling Open(...) again with force=false
                        PvpManager.Instance.Open(0, false);
                        DisplayMessage(client,
                            LanguageMgr.GetTranslation(client.Account.Language,
                                "RvRManager.PvPWillCloseAutomatically"));
                        break;
                    }

                case "status":
                    {
                        // Show the PvP status
                        // Example text
                        string status = PvpManager.Instance.IsOpen
                            ? "open, region param " + PvpManager.Instance.Region +
                              " session zones: " + string.Join(",", PvpManager.Instance.Maps)
                            : "close";
                        DisplayMessage(client,
                            LanguageMgr.GetTranslation(client.Account.Language,
                                "RvRManager.PvPStatus", status));
                        break;
                    }

                case "refresh":
                    {
                        if (PvpManager.Instance.IsOpen)
                        {
                            DisplayMessage(client,
                                LanguageMgr.GetTranslation(client.Account.Language,
                                    "RvRManager.PvPMustBeClosed"));
                            break;
                        }
                        // re-scan or reload DB sessions
                        PvpSessionMgr.ReloadSessions();
                        var pvpMaps = string.Join(", ", PvpManager.Instance.FindPvPMaps());
                        DisplayMessage(client,
                            LanguageMgr.GetTranslation(client.Account.Language,
                                "RvRManager.PvPMapsUsed", "???", pvpMaps));
                        break;
                    }

                case "info":
                    {
                        // Show PvP scoreboard or session infos
                        var stats = PvpManager.Instance.GetStatistics(client.Player);
                        client.Out.SendCustomTextWindow("PvP Info", stats);
                        break;
                    }

                case "reset":
                    PvpManager.Instance.Close();
                    PvpManager.Instance.Open(0, false);
                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.PvPReset"));
                    break;

                default:
                    DisplayCmd(client);
                    break;
            }
        }

        private void DisplayCmd(GameClient client)
        {
            if (client.Account.PrivLevel > 1)
            {
                // For GMs and admins, show the full list.
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.PvP.Description"),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.PvP.Usage.Info"),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.PvP.Usage.Open"),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.PvP.Usage.Close"),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.PvP.Usage.Unforce"),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.PvP.Usage.Status"),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.PvP.Usage.Refresh"),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.PvP.Usage.Reset"),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            else
            {
                // Regular players only see the description and info usage.
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.PvP.Description"),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.PvP.Usage.Info"),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
    }
}