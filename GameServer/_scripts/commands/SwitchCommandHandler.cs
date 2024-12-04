using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DOL.Language;
using DOL.Database;

namespace DOL.GS.Commands
{
    [Cmd("&switch",
        ePrivLevel.Player,
        "Commands.Players.SwitchCommand.Usage",
        "Commands.Players.SwitchCommand.1h",
        "Commands.Players.SwitchCommand.Offhand",
        "Commands.Players.SwitchCommand.2h",
        "Commands.Players.SwitchCommand.Range")]
    public class SwitchCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 3)
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client, "Commands.Players.SwitchCommand.SelectSlot"));
                DisplaySyntax(client);
                return;
            }

            eInventorySlot ToSlot = eInventorySlot.FirstBackpack;

            switch (args[1])
            {
                case "1h":
                case "1main":
                    ToSlot = eInventorySlot.RightHandWeapon;
                    break;
                case "2h":
                case "2mains":
                    ToSlot = eInventorySlot.TwoHandWeapon;
                    break;
                case "offhand":
                case "maingauche":
                    ToSlot = eInventorySlot.LeftHandWeapon;
                    break;
                case "range":
                case "distance":
                    ToSlot = eInventorySlot.DistanceWeapon;
                    break;
            }

            int fromSlot;

            if (int.TryParse(args[2], out fromSlot))
            {
                if (fromSlot < 1 || fromSlot > 40)
                {
                    DisplayMessage(client, LanguageMgr.GetTranslation(client, "Commands.Players.SwitchCommand.InvalidSlot"));
                    DisplaySyntax(client);
                    return;
                }

                SwitchItem(client.Player, ToSlot, (eInventorySlot)(fromSlot + (int)eInventorySlot.FirstBackpack - 1));
            }
            else
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client, "Commands.Players.SwitchCommand.InvalidSlotNumber"));
                DisplaySyntax(client);
                return;
            }
        }

        public void SwitchItem(GamePlayer player, eInventorySlot ToSlot, eInventorySlot FromSlot)
        {
            InventoryItem item = player.Inventory.GetItem(FromSlot);

            if (item != null)
            {
                if (!GlobalConstants.IsWeapon(item.Object_Type))
                {
                    DisplayMessage(player.Client, LanguageMgr.GetTranslation(player.Client, "Commands.Players.SwitchCommand.NotAWeapon"));
                    DisplaySyntax(player.Client);
                    return;
                }

                if (!player.Inventory.MoveItem(FromSlot, ToSlot, 1))
                {
                    DisplayMessage(player.Client, LanguageMgr.GetTranslation(player.Client, "Commands.Players.SwitchCommand.InvalidType"));
                    DisplaySyntax(player.Client);
                    return;
                }

                DisplayMessage(player.Client, LanguageMgr.GetTranslation(player.Client, "Commands.Players.SwitchCommand.SwitchSuccess", item.Name));
            }
            else
            {
                DisplayMessage(player.Client, LanguageMgr.GetTranslation(player.Client, "Commands.Players.SwitchCommand.NoItemInSlot"));
                DisplaySyntax(player.Client);
            }
        }
    }
}