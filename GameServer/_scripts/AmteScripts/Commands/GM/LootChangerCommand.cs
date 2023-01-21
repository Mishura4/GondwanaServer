using System;
using System.Collections;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&lootchanger",
        ePrivLevel.GM,
        "Commands.GM.LootChanger.Description",
        "Commands.GM.LootChanger.Usage.Add",
        "Commands.GM.LootChanger.Usage.Remove",
        "Commands.GM.LootChanger.Usage.Info")]
    public class LootChangerCommand : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (LootChangerGenerator.AddOrChangeLoot(client.Player.TargetObject as GameNPC, client, args) == -1)
                DisplaySyntax(client);
        }
    }
}
