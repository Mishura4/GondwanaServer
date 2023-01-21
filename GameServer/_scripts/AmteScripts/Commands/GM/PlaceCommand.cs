using DOL.GS.Scripts;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
         "&place",
         ePrivLevel.GM,
         "Commands.GM.Place.Description",
         "Commands.GM.Place.Usage.Create")]
    public class PlaceAssiseCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            switch (args[1].ToLower())
            {
                case "create":
                    PlaceAssise p = new PlaceAssise();
                    p.Position = client.Player.Position;
                    p.CurrentRegion = client.Player.CurrentRegion;
                    p.Heading = client.Player.Heading;
                    p.AddToWorld();
                    p.SaveIntoDatabase();
                    break;

                default:
                    DisplaySyntax(client);
                    break;
            }
        }
    }
}
