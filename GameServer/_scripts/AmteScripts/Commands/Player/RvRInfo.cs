using AmteScripts.Managers;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&rvrinfo",
        new[] { "/score" },
		ePrivLevel.Player,
        "Commands.Players.RvRInfo.Description",
        "Commands.Players.RvRInfo.Usage")]
	public class RvRInfoCommandHandler : AbstractCommandHandler, ICommandHandler
	{
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "rvrinfo", 500))
            {
                DisplayMessage(client, "Arrête de spammer la commande ! Tu vas te faire mal aux doigts !");
                return;
            }

            client.Out.SendCustomTextWindow("RvR", RvrManager.Instance.GetStatistics(client.Player));
        }
	}
}