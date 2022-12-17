using DOL.GS;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
	  "&housepoints",
	  ePrivLevel.Player,
        "Commands.Players.Housepoint.Description",
        "Commands.Players.Housepoint.Usage")]
	public class HousePointsCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
            House house = client.Player.CurrentHouse;
			if (!client.Player.InHouse || house == null)
			{
                DisplayMessage(client, LanguageMgr.GetTranslation(client, "Commands.Players.Housepoint.NotInHouse"));
				return;
			}

            if (!house.HasOwnerPermissions(client.Player))
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client, "Commands.Players.Housepoint.NoPermission"));
                return;
            }

			client.Player.Out.SendToggleHousePoints(house);
		}
	}
}