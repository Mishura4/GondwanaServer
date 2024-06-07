using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    /// <summary>
    /// command handler for /boat command
    /// </summary>
    [Cmd(
        "&boat",
        new string[] { "&boatcommand" },
        ePrivLevel.Player,
        "Commands.Players.Boat.Description",
        "Commands.Players.Boat.Usage")]
    public class BoatCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        /// <summary>
        /// method to handle /boat commands from a client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "boat"))
                return;

            try
            {
                switch (args[1])
                {
                    case "summon":
                        {
                            if (!client.Player.IsSwimming)
                            {
                                // Not in water
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.NotInWater"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            // Check to see if player has boat
                            int boatFound = 0;
                            GameBoat curBoat = BoatMgr.GetBoatByOwner(client.Player.InternalID);
                            if (curBoat != null)
                            {
                                if (curBoat.OwnerID == client.Player.InternalID)
                                    boatFound = 1;
                                else
                                    curBoat = null;
                            }
                            else
                                curBoat = null;

                            if (curBoat == null && boatFound != 1)
                            {
                                if (GameBoat.PlayerHasItem(client.Player, "scout_boat"))
                                {
                                    GameBoat playerBoat = new GameBoat();
                                    InventoryItem item = client.Player.Inventory.GetFirstItemByID("scout_boat", eInventorySlot.Min_Inv, eInventorySlot.Max_Inv);
                                    playerBoat.BoatID = System.Guid.NewGuid().ToString();
                                    playerBoat.Name = LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Name.Scout_Boat", client.Player.Name);
                                    playerBoat.Position = client.Player.Position;
                                    playerBoat.Model = 2648;
                                    playerBoat.Realm = client.Player.Realm;
                                    playerBoat.OwnerID = client.Player.InternalID;
                                    playerBoat.MaxSpeedBase = 500;
                                    client.Player.Inventory.RemoveItem(item);
                                    InventoryLogging.LogInventoryAction(client.Player, "", "(ground)", eInventoryActionType.Other, item, item.Count);
                                    playerBoat.Riders = new GamePlayer[8];
                                    BlankBrain brain = new BlankBrain();
                                    playerBoat.SetOwnBrain(brain);
                                    playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
                                    if (client.Player.Guild != null)
                                    {
                                        if (client.Player.Guild.Emblem != 0)
                                            playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

                                        playerBoat.GuildName = client.Player.Guild.Name;
                                    }
                                    playerBoat.AddToWorld();
                                    client.Player.MountSteed(playerBoat, true);
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Summoned", playerBoat.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                else if (GameBoat.PlayerHasItem(client.Player, "warship"))
                                {
                                    GameBoat playerBoat = new GameBoat();
                                    InventoryItem item = client.Player.Inventory.GetFirstItemByID("warship", eInventorySlot.Min_Inv, eInventorySlot.Max_Inv);
                                    playerBoat.BoatID = System.Guid.NewGuid().ToString();
                                    playerBoat.Name = LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Name.Warship", client.Player.Name);
                                    playerBoat.Position = client.Player.Position;
                                    playerBoat.Model = 2647;
                                    playerBoat.Realm = client.Player.Realm;
                                    playerBoat.OwnerID = client.Player.InternalID;
                                    playerBoat.MaxSpeedBase = 400;
                                    client.Player.Inventory.RemoveItem(item);
                                    InventoryLogging.LogInventoryAction(client.Player, "", "(ground)", eInventoryActionType.Other, item, item.Count);
                                    playerBoat.Riders = new GamePlayer[32];
                                    BlankBrain brain = new BlankBrain();
                                    playerBoat.SetOwnBrain(brain);
                                    playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
                                    if (client.Player.Guild != null)
                                    {
                                        if (client.Player.Guild.Emblem != 0)
                                            playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

                                        playerBoat.GuildName = client.Player.Guild.Name;
                                    }
                                    playerBoat.AddToWorld();
                                    client.Player.MountSteed(playerBoat, true);
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Summoned", playerBoat.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                else if (GameBoat.PlayerHasItem(client.Player, "galleon"))
                                {
                                    GameBoat playerBoat = new GameBoat();
                                    InventoryItem item = client.Player.Inventory.GetFirstItemByID("galleon", eInventorySlot.Min_Inv, eInventorySlot.Max_Inv);
                                    playerBoat.BoatID = System.Guid.NewGuid().ToString();
                                    playerBoat.Name = LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Name.Galleon", client.Player.Name);
                                    playerBoat.Model = 2646;
                                    playerBoat.Realm = client.Player.Realm;
                                    playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
                                    playerBoat.OwnerID = client.Player.InternalID;
                                    playerBoat.MaxSpeedBase = 300;
                                    client.Player.Inventory.RemoveItem(item);
                                    InventoryLogging.LogInventoryAction(client.Player, "", "(ground)", eInventoryActionType.Other, item, item.Count);
                                    playerBoat.Riders = new GamePlayer[16];
                                    BlankBrain brain = new BlankBrain();
                                    playerBoat.SetOwnBrain(brain);
                                    playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
                                    if (client.Player.Guild != null)
                                    {
                                        if (client.Player.Guild.Emblem != 0)
                                            playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

                                        playerBoat.GuildName = client.Player.Guild.Name;
                                    }
                                    playerBoat.AddToWorld();
                                    client.Player.MountSteed(playerBoat, true);
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Summoned", playerBoat.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                else if (GameBoat.PlayerHasItem(client.Player, "skiff"))
                                {
                                    GameBoat playerBoat = new GameBoat();
                                    InventoryItem item = client.Player.Inventory.GetFirstItemByID("skiff", eInventorySlot.Min_Inv, eInventorySlot.Max_Inv);
                                    playerBoat.BoatID = System.Guid.NewGuid().ToString();
                                    playerBoat.Name = LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Name.Skiff", client.Player.Name);
                                    playerBoat.Model = 1616;
                                    playerBoat.Realm = client.Player.Realm;
                                    playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
                                    playerBoat.OwnerID = client.Player.InternalID;
                                    playerBoat.MaxSpeedBase = 250;
                                    client.Player.Inventory.RemoveItem(item);
                                    InventoryLogging.LogInventoryAction(client.Player, "", "(ground)", eInventoryActionType.Other, item, item.Count);
                                    playerBoat.Riders = new GamePlayer[8];
                                    BlankBrain brain = new BlankBrain();
                                    playerBoat.SetOwnBrain(brain);
                                    playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
                                    if (client.Player.Guild != null)
                                    {
                                        if (client.Player.Guild.Emblem != 0)
                                            playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

                                        playerBoat.GuildName = client.Player.Guild.Name;
                                    }
                                    playerBoat.AddToWorld();
                                    client.Player.MountSteed(playerBoat, true);
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Summoned", playerBoat.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                else if (GameBoat.PlayerHasItem(client.Player, "Viking_Longship"))
                                {
                                    GameBoat playerBoat = new GameBoat();
                                    InventoryItem item = client.Player.Inventory.GetFirstItemByID("Viking_Longship", eInventorySlot.Min_Inv, eInventorySlot.Max_Inv);
                                    playerBoat.BoatID = System.Guid.NewGuid().ToString();
                                    playerBoat.Name = LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Name.Viking_Longship", client.Player.Name);
                                    playerBoat.Model = 1615;
                                    playerBoat.Realm = client.Player.Realm;
                                    playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
                                    playerBoat.OwnerID = client.Player.InternalID;
                                    playerBoat.MaxSpeedBase = 500;
                                    client.Player.Inventory.RemoveItem(item);
                                    InventoryLogging.LogInventoryAction(client.Player, "", "(ground)", eInventoryActionType.Other, item, item.Count);
                                    playerBoat.Riders = new GamePlayer[32];
                                    BlankBrain brain = new BlankBrain();
                                    playerBoat.SetOwnBrain(brain);
                                    playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
                                    if (client.Player.Guild != null)
                                    {
                                        if (client.Player.Guild.Emblem != 0)
                                            playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

                                        playerBoat.GuildName = client.Player.Guild.Name;
                                    }
                                    playerBoat.AddToWorld();
                                    client.Player.MountSteed(playerBoat, true);
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Summoned", playerBoat.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                else if (GameBoat.PlayerHasItem(client.Player, "ps_longship"))
                                {
                                    GameBoat playerBoat = new GameBoat();
                                    InventoryItem item = client.Player.Inventory.GetFirstItemByID("ps_longship", eInventorySlot.Min_Inv, eInventorySlot.Max_Inv);
                                    playerBoat.BoatID = System.Guid.NewGuid().ToString();
                                    playerBoat.Name = LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Name.Longship", client.Player.Name);
                                    playerBoat.Model = 1595;
                                    playerBoat.Realm = client.Player.Realm;
                                    playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
                                    playerBoat.OwnerID = client.Player.InternalID;
                                    playerBoat.MaxSpeedBase = 600;
                                    client.Player.Inventory.RemoveItem(item);
                                    InventoryLogging.LogInventoryAction(client.Player, "", "(ground)", eInventoryActionType.Other, item, item.Count);
                                    playerBoat.Riders = new GamePlayer[31];
                                    BlankBrain brain = new BlankBrain();
                                    playerBoat.SetOwnBrain(brain);
                                    playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
                                    if (client.Player.Guild != null)
                                    {
                                        if (client.Player.Guild.Emblem != 0)
                                            playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

                                        playerBoat.GuildName = client.Player.Guild.Name;
                                    }
                                    playerBoat.AddToWorld();
                                    client.Player.MountSteed(playerBoat, true);
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Summoned", playerBoat.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                else if (GameBoat.PlayerHasItem(client.Player, "stygian_ship"))
                                {
                                    GameBoat playerBoat = new GameBoat();
                                    InventoryItem item = client.Player.Inventory.GetFirstItemByID("stygian_ship", eInventorySlot.Min_Inv, eInventorySlot.Max_Inv);
                                    playerBoat.BoatID = System.Guid.NewGuid().ToString();
                                    playerBoat.Name = LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Name.Stygian_Ship", client.Player.Name);
                                    playerBoat.Model = 1612;
                                    playerBoat.Realm = client.Player.Realm;
                                    playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
                                    playerBoat.OwnerID = client.Player.InternalID;
                                    playerBoat.MaxSpeedBase = 500;
                                    client.Player.Inventory.RemoveItem(item);
                                    InventoryLogging.LogInventoryAction(client.Player, "", "(ground)", eInventoryActionType.Other, item, item.Count);
                                    playerBoat.Riders = new GamePlayer[24];
                                    BlankBrain brain = new BlankBrain();
                                    playerBoat.SetOwnBrain(brain);
                                    playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
                                    if (client.Player.Guild != null)
                                    {
                                        if (client.Player.Guild.Emblem != 0)
                                            playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

                                        playerBoat.GuildName = client.Player.Guild.Name;
                                    }
                                    playerBoat.AddToWorld();
                                    client.Player.MountSteed(playerBoat, true);
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Summoned", playerBoat.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                else if (GameBoat.PlayerHasItem(client.Player, "atlantean_ship"))
                                {
                                    GameBoat playerBoat = new GameBoat();
                                    InventoryItem item = client.Player.Inventory.GetFirstItemByID("atlantean_ship", eInventorySlot.Min_Inv, eInventorySlot.Max_Inv);
                                    playerBoat.BoatID = System.Guid.NewGuid().ToString();
                                    playerBoat.Name = LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Name.Atlantean_Ship", client.Player.Name);
                                    playerBoat.Model = 1613;
                                    playerBoat.Realm = client.Player.Realm;
                                    playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
                                    playerBoat.OwnerID = client.Player.InternalID;
                                    playerBoat.MaxSpeedBase = 800;
                                    client.Player.Inventory.RemoveItem(item);
                                    InventoryLogging.LogInventoryAction(client.Player, "", "(ground)", eInventoryActionType.Other, item, item.Count);
                                    playerBoat.Riders = new GamePlayer[64];
                                    BlankBrain brain = new BlankBrain();
                                    playerBoat.SetOwnBrain(brain);
                                    playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
                                    if (client.Player.Guild != null)
                                    {
                                        if (client.Player.Guild.Emblem != 0)
                                            playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

                                        playerBoat.GuildName = client.Player.Guild.Name;
                                    }
                                    playerBoat.AddToWorld();
                                    client.Player.MountSteed(playerBoat, true);
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Summoned", playerBoat.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                else if (GameBoat.PlayerHasItem(client.Player, "British_Cog"))
                                {
                                    GameBoat playerBoat = new GameBoat();
                                    InventoryItem item = client.Player.Inventory.GetFirstItemByID("British_Cog", eInventorySlot.Min_Inv, eInventorySlot.Max_Inv);
                                    playerBoat.BoatID = System.Guid.NewGuid().ToString();
                                    playerBoat.Name = LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Name.British_Cog", client.Player.Name);
                                    playerBoat.Model = 1614;
                                    playerBoat.Realm = client.Player.Realm;
                                    playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
                                    playerBoat.OwnerID = client.Player.InternalID;
                                    playerBoat.MaxSpeedBase = 700;
                                    client.Player.Inventory.RemoveItem(item);
                                    InventoryLogging.LogInventoryAction(client.Player, "", "(ground)", eInventoryActionType.Other, item, item.Count);
                                    playerBoat.Riders = new GamePlayer[33];
                                    BlankBrain brain = new BlankBrain();
                                    playerBoat.SetOwnBrain(brain);
                                    playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
                                    if (client.Player.Guild != null)
                                    {
                                        if (client.Player.Guild.Emblem != 0)
                                            playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

                                        playerBoat.GuildName = client.Player.Guild.Name;
                                    }
                                    playerBoat.AddToWorld();
                                    client.Player.MountSteed(playerBoat, true);
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Summoned", playerBoat.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                else
                                {
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.NotOwnBoat"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                                BoatMgr.SaveAllBoats();
                            }
                            else if (boatFound == 1)
                            {
                                if (client.Player.Guild != null)
                                {
                                    if (client.Player.Guild.Emblem != 0)
                                        curBoat.Emblem = (ushort)client.Player.Guild.Emblem;

                                    curBoat.GuildName = client.Player.Guild.Name;
                                }

                                curBoat.Position = client.Player.Position;
                                curBoat.Realm = client.Player.Realm;
                                curBoat.Riders = new GamePlayer[32];
                                BlankBrain brain = new BlankBrain();
                                curBoat.SetOwnBrain(brain);
                                curBoat.AddToWorld();
                                client.Player.MountSteed(curBoat, true);
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Summoned", curBoat.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.NotOwnBoat"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            break;
                        }
                    case "unsummon":
                        {
                            GameBoat playerBoat = BoatMgr.GetBoatByOwner(client.Player.InternalID);

                            if (playerBoat != null)
                            {
                                if (client.Player.InternalID == playerBoat.OwnerID)
                                {
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Unsummoned", playerBoat.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                    playerBoat.SaveIntoDatabase();
                                    playerBoat.RemoveFromWorld();
                                }
                            }
                            else
                            {
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.NotOwnBoat"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            break;
                        }
                    case "board":
                        {
                            GameBoat playerBoat = BoatMgr.GetBoatByName(client.Player.TargetObject.Name);
                            if (client.Player.TargetObject == null)
                            {
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.NoBoatSelected"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                break;
                            }

                            if (playerBoat.MAX_PASSENGERS > 1)
                            {
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.YouBoard", playerBoat.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Player.MountSteed(playerBoat, true);
                            }
                            else
                            {
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.FullBoat"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            break;
                        }
                    case "follow":
                        {
                            GameBoat targetBoat = BoatMgr.GetBoatByName(client.Player.TargetObject.Name);

                            if (client.Player.Steed.OwnerID == client.Player.InternalID)// needs to be player on own boat
                            {
                                if (client.Player.TargetObject == null)
                                {
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.NoBoatSelected"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                    break;
                                }

                                client.Player.Steed.Follow(targetBoat, 800, 5000);
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.MoveFollow", client.Player.TargetObject.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.NotOwnBoat"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            break;
                        }
                    case "stopfollow":
                        {
                            if (client.Player.Steed.OwnerID == client.Player.InternalID)// needs to be player on own boat
                            {
                                client.Player.Steed.StopFollowing();
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.StopFollow"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.NotOwnBoat"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            break;
                        }
                    case "invite":
                        {
                            break;
                        }
                    case "delete":
                        {
                            if (client.Player.TargetObject == null)
                            {
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.NoBoatSelected"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                break;
                            }
                            GameBoat playerBoat = BoatMgr.GetBoatByName(client.Player.TargetObject.Name);

                            if (client.Player.InternalID == playerBoat.OwnerID)
                                client.Player.Out.SendCustomDialog(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.DeleteConfirmation", playerBoat.Name), new CustomDialogResponse(BoatDeleteConfirmation));
                            else
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.NotOwnBoat"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

                            break;
                        }
                    case "boot":
                        {
                            GameBoat playerBoat = BoatMgr.GetBoatByOwner(client.Player.InternalID);

                            if (client.Player.InternalID == playerBoat.OwnerID)
                            {
                                if (client.Player.TargetObject == null)
                                {
                                    // no player selected
                                    break;
                                }

                                GamePlayer target = (client.Player.TargetObject as GamePlayer);
                                if (playerBoat.RiderSlot(target) != -1)
                                {
                                    target.DismountSteed(true);
                                    target.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.BootedBy", client.Player.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.BootedTarget", target.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                else
                                {
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.TargetNotInBoat", target.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                            }
                            else
                            {
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.NotOwnBoat"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            break;
                        }
                    default:
                        {
                            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.UnknownCommand"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            DisplayHelp(client);
                        }
                        break;
                }
            }
            catch (Exception)
            {
                DisplayHelp(client);
            }
        }
        public void DisplayHelp(GameClient client)
        {
            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Usage"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Help.Summon"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Help.Unsummon"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Help.Follow"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Help.StopFollow"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Help.Board"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Help.Boot"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Boat.Help.Delete"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
        }

        protected void BoatDeleteConfirmation(GamePlayer player, byte response)
        {
            if (response != 0x01) return;

            GameBoat playerBoat = BoatMgr.GetBoatByOwner(player.InternalID);

            playerBoat.RemoveFromWorld();
            BoatMgr.DeleteBoat(playerBoat.Name);
        }
    }
}
