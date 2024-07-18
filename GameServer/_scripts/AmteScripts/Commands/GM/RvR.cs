using AmteScripts.Managers;
using AmteScripts.Utils;
using System.Linq;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&rvr",
        ePrivLevel.GM,
        "Commands.GM.RvR.Description",
        "Commands.GM.RvR.Usage.Open",
        "Commands.GM.RvR.Usage.Close",
        "Commands.GM.RvR.Usage.Unforce",
        "Commands.GM.RvR.Usage.OpenPvP",
        "Commands.GM.RvR.Usage.ClodePvP",
        "Commands.GM.RvR.Usage.Status",
        "Commands.GM.RvR.Usage.UnforcePvP",
        "Commands.GM.RvR.Usage.Refresh")]
    public class RvRCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length <= 1)
            {
                DisplaySyntax(client);
                return;
            }

            ushort region = 0;
            switch (args[1].ToLower())
            {
                case "open":
                    if (RvrManager.Instance.Open(true))
                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.RvROpened", string.Join("-", RvrManager.Instance.Regions.OrderBy(r => r))));
                    else
                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.RvRNotOpened"));
                    break;
                case "openpvp":
                    if (args.Length >= 3 && !ushort.TryParse(args[2], out region))
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    if (PvpManager.Instance.Open(region, true))
                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.PvPOpened", PvpManager.Instance.Region));
                    else
                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.PvPNotOpened", region));
                    break;

                case "close":
                    DisplayMessage(client, RvrManager.Instance.Close() ? LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.RvRClosed") : LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.RvRNotClosed"));
                    break;

                case "closepvp":
                    DisplayMessage(client, PvpManager.Instance.Close() ? LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.PvPClosed") : LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.PvPNotClosed"));
                    break;

                case "unforce":
                    if (!RvrManager.Instance.IsOpen)
                    {
                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.RvRUnforceNotPossible"));
                        break;
                    }
                    RvrManager.Instance.Open(false);
                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.RvRWillCloseAutomatically"));
                    break;

                case "unforcepvp":
                    if (!PvpManager.Instance.IsOpen)
                    {
                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.PvPUnforceNotPossible"));
                        break;
                    }
                    PvpManager.Instance.Open(0, false);
                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.PvPWillCloseAutomatically"));
                    break;

                case "status":
                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.RvRStatus", RvrManager.Instance.IsOpen ? "open, les regions sont: " + string.Join("-", RvrManager.Instance.Regions) + "." : "close"));
                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.PvPStatus", PvpManager.Instance.IsOpen ? "open, les regions sont: " + string.Join(",", PvpManager.Instance.Maps) + "." : "close"));
                    break;

                case "refresh":
                    if (RvrManager.Instance.IsOpen || PvpManager.Instance.IsOpen)
                    {
                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.RvRAndPvPMustBeClosed"));
                        break;
                    }
                    var rvr = string.Join(", ", RvrManager.Instance.InitMapsAndTerritories());
                    var pvp = string.Join(", ", PvpManager.Instance.FindPvPMaps());
                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.MapsUsed", rvr, pvp));
                    break;
                case "reset":
                    PvpManager.Instance.Close();
                    RvrManager.Instance.Close();
                    RvrManager.Instance.Open(false);
                    PvpManager.Instance.Open(0, false);

                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "RvRManager.RvRPvPReset"));
                    break;
            }
        }
    }
}
