using DOL.GS;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
      "&houseface",
      ePrivLevel.Player,
        "Commands.Players.Housepoint.Description")]
    public class HouseFaceCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            int housenumber = 0;
            if (args.Length > 1)
            {
                try
                {
                    housenumber = int.Parse(args[1]);
                }
                catch
                {
                    DisplaySyntax(client);
                    return;
                }
            }
            else HouseMgr.GetHouseNumberByPlayer(client.Player);

            if (housenumber == 0)
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client, "Commands.Players.Houseface.NotFound"));
                return;
            }

            House house = HouseMgr.GetHouse(housenumber);

            client.Player.TurnTo(house.Position.Coordinate);
            client.Out.SendPlayerJump(true);
            DisplayMessage(client, LanguageMgr.GetTranslation(client, "Commands.Players.Houseface.Faced", housenumber));
        }
    }
}