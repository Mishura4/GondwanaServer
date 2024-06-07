/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Linq;
using System.Reflection;

using DOL.GS;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS.Profession;

namespace DOL.GS.Commands
{
    [CmdAttribute("&merchant",
         ePrivLevel.GM,
         "Commands.GM.Merchant.Description",
         //Usage
         "Commands.GM.Merchant.Usage.Create",
         "Commands.GM.Merchant.Usage.CreateType",
         "Commands.GM.Merchant.Usage.Info",
         "Commands.GM.Merchant.Usage.Save",
         "'/merchant savelist <newname>' to saves this merchants items list under a new name",
         "Commands.GM.Merchant.Usage.Remove",
         "Commands.GM.Merchant.Usage.Sell",
         "Commands.GM.Merchant.Usage.SellRemove",
         "Commands.GM.Merchant.Usage.Articles.Add",
         "Commands.GM.Merchant.Usage.Articles.Remove",
         "Commands.GM.Merchant.Usage.Articles.Delete",
         "Commands.GM.Merchant.Usage.Type")]
    public class MerchantCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length == 1)
            {
                DisplaySyntax(client);
                return;
            }

            string param = "";
            if (args.Length > 2)
                param = String.Join(" ", args, 2, args.Length - 2);

            GameMerchant targetMerchant = null;
            if (client.Player.TargetObject != null && client.Player.TargetObject is GameMerchant)
                targetMerchant = (GameMerchant)client.Player.TargetObject;

            if (args[1].ToLower() != "create" && targetMerchant == null)
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.CommandOverview"));
                return;
            }

            switch (args[1].ToLower())
            {
                #region Create
                case "create":
                    {
                        bool isRenaissance = false;
                        string theType = "DOL.GS.GameMerchant";

                        if (args.Length > 2)
                        {
                            if (!bool.TryParse(args[2], out isRenaissance))
                            {
                                theType = args[2];
                            }

                            if (args.Length > 3)
                            {
                                bool.TryParse(args[3], out isRenaissance);
                                theType = args[2];
                            }
                        }

                        //Create a new merchant
                        GameMerchant merchant = null;
                        foreach (Assembly script in ScriptMgr.GameServerScripts)
                        {
                            try
                            {
                                client.Out.SendDebugMessage(script.FullName);
                                merchant = (GameMerchant)script.CreateInstance(theType, false);
                                if (merchant != null)
                                    break;
                            }
                            catch (Exception e)
                            {
                                client.Out.SendMessage(e.ToString(), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                            }
                        }
                        if (merchant == null)
                        {
                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.ErrorCreateInstance", theType));
                            return;
                        }
                        //Fill the object variables
                        merchant.Position = client.Player.Position;
                        merchant.Level = 1;
                        merchant.Realm = client.Player.Realm;
                        merchant.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "Commands.GM.Merchant.NewName");
                        merchant.Model = 9;
                        //Fill the living variables
                        merchant.MaxSpeedBase = 200;
                        merchant.GuildName = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "Commands.GM.Merchant.NewGuildName");
                        merchant.Size = 50;
                        merchant.IsRenaissance = isRenaissance;
                        merchant.AddToWorld();
                        merchant.LoadedFromScript = false; // allow saving
                        merchant.SaveIntoDatabase();
                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Create.Created", merchant.ObjectID));
                        break;
                    }
                #endregion Create
                #region Info
                case "info":
                    {
                        if (args.Length == 2)
                        {
                            if (targetMerchant.Catalog.IsEmpty)
                                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Info.ArtListIsEmpty"));
                            else
                            {
                                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Merchant.Info.ArtList", targetMerchant.Catalog.ItemListId));
                            }

                        }
                        break;
                    }
                #endregion Info
                #region Save
                case "save":
                    {
                        targetMerchant.SaveIntoDatabase();
                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Save.SavedInDB"));
                        break;
                    }
                #endregion Save
                #region SaveList
                case "savelist":
                    {
                        string currentID = targetMerchant.Catalog.ItemListId;

                        var itemList = DOLDB<MerchantItem>.SelectObjects(DB.Column(nameof(MerchantItem.ItemListID)).IsEqualTo(currentID));
                        foreach (MerchantItem merchantItem in itemList)
                        {
                            MerchantItem item = new MerchantItem();
                            item.ItemListID = GameServer.Database.Escape(args[2]);
                            item.ItemTemplateID = merchantItem.ItemTemplateID;
                            item.PageNumber = merchantItem.PageNumber;
                            item.SlotPosition = merchantItem.SlotPosition;
                            GameServer.Database.AddObject(item);
                        }

                        DisplayMessage(client, "New MerchantItems list saved as '" + GameServer.Database.Escape(args[2]) + "'");

                        break;
                    }
                #endregion SaveList
                #region Remove
                case "remove":
                    {
                        targetMerchant.DeleteFromDatabase();
                        targetMerchant.Delete();
                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Remove.RemovedFromDB"));
                        break;
                    }
                #endregion Remove
                #region Sell
                case "sell":
                    {
                        switch (args[2].ToLower())
                        {
                            #region Add
                            case "add":
                                {
                                    if (args.Length == 4)
                                    {
                                        try
                                        {
                                            string templateID = args[3];
                                            targetMerchant.Catalog = MerchantCatalog.Create(templateID);
                                            targetMerchant.SaveIntoDatabase();
                                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Sell.Add.Loaded"));
                                        }
                                        catch (Exception)
                                        {
                                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.CommandOverview"));
                                            return;
                                        }
                                    }
                                    break;
                                }
                            #endregion Add
                            #region Remove
                            case "remove":
                                {
                                    if (args.Length == 3)
                                    {
                                        targetMerchant.Catalog = MerchantCatalog.Create();
                                        targetMerchant.SaveIntoDatabase();
                                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Sell.Remove.Removed"));
                                    }
                                    break;
                                }
                            #endregion Remove
                            #region Default
                            default:
                                {
                                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.CommandOverview"));
                                    return;
                                }
                                #endregion Default
                        }
                        break;
                    }
                #endregion Sell
                #region Articles
                case "articles":
                    {
                        if (args.Length < 3)
                        {
                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.CommandOverview"));
                            return;
                        }

                        switch (args[2].ToLower())
                        {
                            #region Add
                            case "add":
                                {
                                    if (args.Length <= 6)
                                    {
                                        try
                                        {
                                            string templateID = args[3];
                                            int page = Convert.ToInt32(args[4]);
                                            eMerchantWindowSlot slot = eMerchantWindowSlot.FirstEmptyInPage;

                                            ItemTemplate template = GameServer.Database.FindObjectByKey<ItemTemplate>(templateID);
                                            if (template == null)
                                            {
                                                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Articles.Add.ItemTemplateNoFound", templateID));
                                                return;
                                            }

                                            if (args.Length == 6)
                                            {
                                                slot = (eMerchantWindowSlot)Convert.ToInt32(args[5]);
                                            }

                                            var catalog = MerchantCatalog.LoadFromDatabase(targetMerchant.Catalog.ItemListId);
                                            if (slot == eMerchantWindowSlot.LastInPage)
                                            {
                                                slot = (eMerchantWindowSlot)catalog.GetPage(page).GetNextFreeSlot();
                                            }
                                            var itemCanBeAdded = catalog.GetPage(page).AddItem(template, (byte)slot, template.Price);
                                            if (!itemCanBeAdded)
                                            {
                                                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Articles.Add.PageAndSlotInvalid", page, (MerchantTradeItems.MAX_PAGES_IN_TRADEWINDOWS - 1), slot, (MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS - 1)));
                                                return;
                                            }

                                            var item = DOLDB<MerchantItem>.SelectObject(DB.Column(nameof(MerchantItem.ItemListID)).IsEqualTo(targetMerchant.Catalog.ItemListId).And(DB.Column(nameof(MerchantItem.PageNumber)).IsEqualTo(page)).And(DB.Column(nameof(MerchantItem.SlotPosition)).IsEqualTo(slot)));
                                            if (item == null)
                                            {
                                                item = new MerchantItem();
                                                item.ItemListID = targetMerchant.Catalog.ItemListId;
                                                item.ItemTemplateID = templateID;
                                                item.SlotPosition = (int)slot;
                                                item.PageNumber = page;

                                                GameServer.Database.AddObject(item);
                                            }
                                            else
                                            {
                                                item.ItemTemplateID = templateID;
                                                GameServer.Database.SaveObject(item);
                                            }
                                            if (!targetMerchant.LoadedFromScript)
                                            {
                                                targetMerchant.RemoveFromWorld();
                                                targetMerchant.LoadFromDatabase(GameServer.Database.FindObjectByKey<Mob>(targetMerchant.InternalID));
                                                targetMerchant.AddToWorld();
                                            }
                                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Articles.Add.ItemAdded"));
                                        }
                                        catch (Exception)
                                        {
                                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.CommandOverview"));
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.CommandOverview"));
                                    }
                                    break;
                                }
                            #endregion Add
                            #region Remove
                            case "remove":
                                {
                                    if (args.Length == 5)
                                    {
                                        try
                                        {
                                            int page = Convert.ToInt32(args[3]);
                                            int slot = Convert.ToInt32(args[4]);

                                            if (page < 0 || page >= MerchantTradeItems.MAX_PAGES_IN_TRADEWINDOWS)
                                            {
                                                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Articles.Remove.PageInvalid", page, (MerchantTradeItems.MAX_PAGES_IN_TRADEWINDOWS - 1)));
                                                return;
                                            }

                                            if (slot < 0 || slot >= MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS)
                                            {
                                                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Articles.Remove.SlotInvalid", slot, (MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS - 1)));
                                                return;
                                            }

                                            if (targetMerchant.Catalog.IsEmpty)
                                            {
                                                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Articles.ListNoFound"));
                                                return;
                                            }

                                            MerchantItem item = DOLDB<MerchantItem>.SelectObject(DB.Column(nameof(MerchantItem.ItemListID)).IsEqualTo(targetMerchant.Catalog.ItemListId).And(DB.Column(nameof(MerchantItem.PageNumber)).IsEqualTo(page)).And(DB.Column(nameof(MerchantItem.SlotPosition)).IsEqualTo(slot)));
                                            if (item == null)
                                            {
                                                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Articles.Remove.SlotInPageIsAEmpty", slot, page));
                                                return;
                                            }
                                            GameServer.Database.DeleteObject(item);
                                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Articles.Remove.SlotInPageCleaned", slot, page));

                                        }
                                        catch (Exception)
                                        {
                                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.CommandOverview"));
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.CommandOverview"));
                                    }
                                    break;
                                }
                            #endregion Remove
                            #region Delete
                            case "delete":
                                {
                                    if (args.Length == 3)
                                    {
                                        try
                                        {
                                            if (targetMerchant.Catalog.IsEmpty)
                                            {
                                                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Articles.ListNoFound"));
                                                return;
                                            }
                                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Articles.Delete.DeletingListTemp"));


                                            var merchantitems = DOLDB<MerchantItem>.SelectObjects(DB.Column(nameof(MerchantItem.ItemListID)).IsEqualTo(targetMerchant.Catalog.ItemListId));
                                            if (merchantitems.Count > 0)
                                            {
                                                GameServer.Database.DeleteObject(merchantitems);
                                            }
                                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Articles.Delete.ListDeleted"));
                                        }
                                        catch (Exception)
                                        {
                                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.CommandOverview"));
                                            return;
                                        }
                                    }
                                    break;
                                }
                            #endregion Delete
                            #region Default
                            default:
                                {
                                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.CommandOverview"));
                                    return;
                                }
                                #endregion Default
                        }
                        break;
                    }
                #endregion Articles
                #region Type
                case "type":
                    {
                        string theType = param;
                        if (args.Length > 2)
                            theType = args[2];

                        //Create a new merchant
                        GameMerchant merchant = null;
                        foreach (Assembly script in ScriptMgr.GameServerScripts)
                        {
                            try
                            {
                                client.Out.SendDebugMessage(script.FullName);
                                merchant = (GameMerchant)script.CreateInstance(theType, false);
                                if (merchant != null)
                                    break;
                            }
                            catch (Exception e)
                            {
                                client.Out.SendMessage(e.ToString(), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                            }
                        }
                        if (merchant == null)
                        {
                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.ErrorCreateInstance", theType));
                            return;
                        }
                        //Fill the object variables
                        merchant.Position = targetMerchant.Position;
                        merchant.Level = targetMerchant.Level;
                        merchant.Realm = targetMerchant.Realm;
                        merchant.Name = targetMerchant.Name;
                        merchant.Model = targetMerchant.Model;
                        //Fill the living variables
                        merchant.MaxSpeedBase = targetMerchant.MaxSpeedBase;
                        merchant.GuildName = targetMerchant.GuildName;
                        merchant.Size = targetMerchant.Size;
                        merchant.Inventory = targetMerchant.Inventory;
                        merchant.EquipmentTemplateID = targetMerchant.EquipmentTemplateID;
                        merchant.Catalog = targetMerchant.Catalog;
                        merchant.AddToWorld();
                        merchant.SaveIntoDatabase();
                        targetMerchant.Delete();
                        targetMerchant.DeleteFromDatabase();
                        DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Merchant.Type.Changed", param));
                        break;
                    }
                    #endregion Type
            }
        }
    }
}