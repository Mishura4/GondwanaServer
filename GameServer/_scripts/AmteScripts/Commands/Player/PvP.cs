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
        "Commands.GM.PvP.Usage.Info",
        "Commands.GM.PvP.Usage.Scores")]
    [Cmd(
        "&pvp",
        ePrivLevel.GM,
        "Commands.GM.PvP.Description",
        "Commands.GM.PvP.Usage.Open",
        "Commands.GM.PvP.Usage.Close",
        "Commands.GM.PvP.Usage.Unforce",
        "Commands.GM.PvP.Usage.Status",
        "Commands.GM.PvP.Usage.Refresh",
        "Commands.GM.PvP.Usage.Reset")]
    public class PvpCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length <= 1)
            {
                DisplaySyntax(client);
                return;
            }

            string subcmd = args[1].ToLower();

            if (client.Account.PrivLevel == 1 && subcmd != "info")
            {
                DisplaySyntax(client);
                return;
            }

            string sessionID = string.Empty;
            switch (subcmd)
            {
                case "open":
                    {
                        if (args.Length >= 3)
                        {
                            sessionID = args[2];
                        }
                        
                        if (PvpManager.Instance.IsOpen)
                        {
                            var msg = LanguageMgr.GetTranslation(
                                client.Account.Language,
                                "Commands.GM.PvP.AlreadyOpen",
                                PvpManager.Instance.CurrentSessionId,
                                string.Join(',', PvpManager.Instance.CurrentZones.Select(z => z.Description + "(" + z.ID + ")"))
                            );
                            DisplayMessage(client, msg);
                            return;
                        }
                        
                        bool success = PvpManager.Instance.Open(sessionID, true);
                        if (success)
                        {
                            var msg = LanguageMgr.GetTranslation(
                                client.Account.Language,
                                "Commands.GM.PvP.PvPOpened",
                                PvpManager.Instance.CurrentSessionId,
                                string.Join(',', PvpManager.Instance.CurrentZones.Select(z => z.Description + "(" + z.ID + ")")),
                                string.Join(',', PvpManager.Instance.CurrentZones.Select(z => z.ZoneRegion).Distinct().Select(r => r.Description + "(" + r.ID + ")"))
                            );
                            DisplayMessage(client, msg);
                        }
                        else
                        {
                            DisplayMessage(
                                client,
                                string.IsNullOrEmpty(sessionID) ? LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.PvP.PvPNotOpened") : LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.PvP.PvPNotFound", sessionID)
                            );
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
                        PvpManager.Instance.Open(string.Empty, false);
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
                            ? "open, zones: " + string.Join(',', PvpManager.Instance.CurrentZones.Select(z => z.Description + "(" + z.ID + ")")) +
                              " regions: " + string.Join(',', PvpManager.Instance.CurrentZones.Select(z => z.ZoneRegion).Distinct().Select(r => r.Description + "(" + r.ID + ")"))
                            : "closed";
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
                        var pvpMaps = string.Join(
                            ", ",
                            PvpSessionMgr.GetAllSessions().Select(s => s.SessionID + " => [" + s.ZoneList + "]")
                        );
                        DisplayMessage(client,
                            LanguageMgr.GetTranslation(client.Account.Language,
                                "RvRManager.PvPMapsUsed", pvpMaps));
                        break;
                    }

                case "info":
                    {
                        // Show PvP scoreboard or session infos
                        var stats = PvpManager.Instance.GetStatistics(client.Player);
                        client.Out.SendCustomTextWindow("PvP Info", stats);
                        break;
                    }

                case "scores":
                    {
                        // Show PvP scoreboard or session infos
                        var stats = PvpManager.Instance.GetStatistics(client.Player, true);
                        client.Out.SendCustomTextWindow("PvP Info", stats);
                        break;
                    }

                case "reset":
                    var previous = PvpManager.Instance.CurrentSession?.SessionID;
                    if (string.IsNullOrEmpty(previous))
                    {
                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.PvPCannotReset", previous));
                        return;
                    }
                    PvpManager.Instance.Close();
                    PvpManager.Instance.Open(previous, false);
                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.PvPReset"));
                    break;

                default:
                    DisplaySyntax(client);
                    break;
            }
        }
    }
}