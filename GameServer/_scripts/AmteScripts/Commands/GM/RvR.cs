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
                        DisplayMessage(client, "Les rvr ont été ouverts avec les régions " + string.Join("-", RvrManager.Instance.Regions.OrderBy(r => r)) + ".");
                    else
                        DisplayMessage(client, "Les rvr n'ont pas pu être ouverts.");
                    break;
                case "openpvp":
                    if (args.Length >= 3 && !ushort.TryParse(args[2], out region))
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    if (PvpManager.Instance.Open(region, true))
                        DisplayMessage(client, "Le pvp a été ouvert avec la région " + PvpManager.Instance.Region + ".");
                    else
                        DisplayMessage(client, "Le pvp n'a pas pu être ouvert sur la région " + region + ".");
                    break;

                case "close":
                    DisplayMessage(client, RvrManager.Instance.Close() ? "Le rvr a été fermé." : "Le rvr n'a pas pu être fermé.");
                    break;

                case "closepvp":
                    DisplayMessage(client, PvpManager.Instance.Close() ? "Le pvp a été fermé." : "Le pvp n'a pas pu être fermé.");
                    break;

                case "unforce":
                    if (!RvrManager.Instance.IsOpen)
                    {
                        DisplayMessage(client, "Le rvr doit être ouvert pour le unforce.");
                        break;
                    }
                    RvrManager.Instance.Open(false);
                    DisplayMessage(client, "Le rvr sera fermé automatiquement s'il n'est plus dans les bonnes horaires.");
                    break;

                case "unforcepvp":
                    if (!PvpManager.Instance.IsOpen)
                    {
                        DisplayMessage(client, "Le pvp doit être ouvert pour le unforce.");
                        break;
                    }
                    PvpManager.Instance.Open(0, false);
                    DisplayMessage(client, "Le pvp sera fermé automatiquement s'il n'est plus dans les bonnes horaires.");
                    break;

                case "status":
                    DisplayMessage(client, "Les RvR sont actuellement: " + (RvrManager.Instance.IsOpen ? "open, les regions sont: " + string.Join("-", RvrManager.Instance.Regions) + "." : "close"));
                    DisplayMessage(client, "Les regions PvP sont actuellement: " + (PvpManager.Instance.IsOpen ? "open, les regions sont: " + string.Join(",", PvpManager.Instance.Maps) + "." : "close"));
                    break;

                case "refresh":
                    if (RvrManager.Instance.IsOpen || PvpManager.Instance.IsOpen)
                    {
                        DisplayMessage(client, "Les rvr et le pvp doivent être fermés pour rafraichir la liste des maps disponibles.");
                        break;
                    }
                    var rvr = string.Join(", ", RvrManager.Instance.InitMapsAndTerritories());
                    var pvp = string.Join(", ", PvpManager.Instance.FindPvPMaps());
                    DisplayMessage(client, string.Format("Le rvr utilise les maps: {0}, le pvp utilise les maps: {1}.", rvr, pvp));
                    break;
                case "reset":
                    PvpManager.Instance.Close();
                    RvrManager.Instance.Close();
                    RvrManager.Instance.Open(false);
                    PvpManager.Instance.Open(0, false);

                    DisplayMessage(client, "Les rvr et le pvp ont été réinitialisés.");
                    break;
            }
        }
    }
}
