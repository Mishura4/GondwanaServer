using DOL.Events;
using DOL.gameobjects.CustomNPC;
using DOL.GS;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.commands.playercommands
{
    [CmdAttribute(
        "&combine",
        ePrivLevel.Player,
        "Commands.Players.Combine.Description",
        "Commands.Players.Combine.Usage"
    )]
    public class Combine : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "combine"))
                return;
            if (args.Length != 2 || args[1] != "list")
            {
                DisplaySyntax(client);
                return;
            }
            GamePlayer player = client.Player;
            ShadowNPC shadow = player.ShadowNPC;
            shadow.MoveToPlayer();
            player.TargetObject = shadow;
            player.Out.SendChangeTarget(shadow);
            GameEventMgr.Notify(GamePlayerEvent.ChangeTarget, player, null);
            shadow.Interact(BuildMessage(client));
        }

        public static string BuildMessage(GameClient client)
        {
            string message = LanguageMgr.GetTranslation(client, "Commands.Players.Combine.List") + ":\n\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Weaponcrafting") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Armorcrafting") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Siegecrafting") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Alchemy") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Metalcrafting") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Leathercrafting") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Clothworking") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Gemcutting") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Herbalcrafting") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Tailoring") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Spellcrafting") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Woodworking") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Fletching") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Bountycrafting") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Cooking") + "\n";
            message += LanguageMgr.GetTranslation(client, "Commands.Players.Combine.Scholar");
            return message;
        }
    }
}