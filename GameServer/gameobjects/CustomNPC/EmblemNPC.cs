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
using DOL.Database;
using DOL.GS.Finance;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    [NPCGuildScript("Guild Emblemeer")]
    public class EmblemNPC : GameNPC
    {
        public const long EMBLEM_COST = 50000;
        private const string EMBLEMIZE_ITEM_WEAK = "emblemise item";

        /// <summary>
        /// Can accept any item
        /// </summary>
        public override bool CanTradeAnyItem
        {
            get { return true; }
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            TurnTo(player, 5000);
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "EmblemNPC.Interact.Text1"), eChatType.CT_System, eChatLoc.CL_ChatWindow);

            return true;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            GamePlayer t = source as GamePlayer;
            if (t == null || item == null)
                return false;

            if (item.Emblem != 0)
            {
                t.Out.SendMessage(LanguageMgr.GetTranslation(t.Client.Account.Language, "EmblemNPC.ResponseDialog01"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (item.Object_Type == (int)eObjectType.Shield || item.Item_Type == Slot.CLOAK)
            {
                if (t.Guild == null)
                {
                    t.Out.SendMessage(LanguageMgr.GetTranslation(t.Client.Account.Language, "EmblemNPC.ResponseDialog02"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
                if (t.Guild.Emblem == 0)
                {
                    t.Out.SendMessage(LanguageMgr.GetTranslation(t.Client.Account.Language, "EmblemNPC.ResponseDialog03"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
                if (t.Level < 20) //if level of player < 20 so can not put emblem
                {
                    if (t.CraftingPrimarySkill == eCraftingSkill.NoCrafting)
                    {
                        t.Out.SendMessage(LanguageMgr.GetTranslation(t.Client.Account.Language, "EmblemNPC.ResponseDialog04"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }
                    else
                    {
                        if (t.GetCraftingSkillValue(t.CraftingPrimarySkill) < 400)
                        {
                            t.Out.SendMessage(LanguageMgr.GetTranslation(t.Client.Account.Language, "EmblemNPC.ResponseDialog05"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return false;
                        }
                    }

                }

                if (!t.Guild.HasRank(t, Guild.eRank.Emblem))
                {
                    t.Out.SendMessage(LanguageMgr.GetTranslation(t.Client.Account.Language, "EmblemNPC.ResponseDialog06"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
                t.TempProperties.setProperty(EMBLEMIZE_ITEM_WEAK, new WeakRef(item));
                t.Out.SendCustomDialog(LanguageMgr.GetTranslation(t.Client.Account.Language, "EmblemNPC.ResponseDialog07"), new CustomDialogResponse(EmblemerDialogResponse));
            }
            else
                t.Out.SendMessage(LanguageMgr.GetTranslation(t.Client.Account.Language, "EmblemNPC.ResponseDialog08"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

            return false;
        }

        protected void EmblemerDialogResponse(GamePlayer player, byte response)
        {
            WeakReference itemWeak =
                (WeakReference)player.TempProperties.getProperty<object>(
                    EMBLEMIZE_ITEM_WEAK,
                    new WeakRef(null)
                    );
            player.TempProperties.removeProperty(EMBLEMIZE_ITEM_WEAK);

            if (response != 0x01)
                return; //declined

            InventoryItem item = (InventoryItem)itemWeak.Target;

            if (item == null || item.SlotPosition == (int)eInventorySlot.Ground
                || item.OwnerID == null || item.OwnerID != player.InternalID)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "EmblemNPC.Interact.Text2"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (!player.RemoveMoney(Currency.Copper.Mint(EMBLEM_COST)))
            {
                InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, EMBLEM_COST);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "EmblemNPC.Interact.Text3"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            item.Emblem = player.Guild.Emblem;
            player.Out.SendInventoryItemsUpdate(new InventoryItem[] { item });
            if (item.SlotPosition < (int)eInventorySlot.FirstBackpack)
                player.UpdateEquipmentAppearance();
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "EmblemNPC.Interact.Text4"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
            return;
        }
    }
}