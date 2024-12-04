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
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Trainer
{
    /// <summary>
    /// Mauler Trainer
    /// </summary>
    [NPCGuildScript("Mauler Trainer", eRealm.Hibernia)]
    public class HiberniaMaulerTrainer : GameTrainer
    {
        public override eCharacterClass TrainedClass
        {
            get { return eCharacterClass.MaulerHib; }
        }

        public const string WEAPON_ID1 = "maulerhib_item_staff";
        public const string WEAPON_ID2 = "maulerhib_item_fist";
        public const string ARMOR_ID1 = "vest_of_the_novice_hib2";
        public const string ARMOR_ID2 = "stoneskin_vest_hib";
        public const string ARMOR_ID3 = "vest_of_five_paws_hib";
        public const string ARMOR_ID4 = "vest_of_the_pugilist_hib";

        /// <summary>
        /// Interact with trainer
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;

            // check if class matches
            if (player.CharacterClass.ID == (int)TrainedClass)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.Interact.Text3", this.Name, player.GetName(0, false)), eChatType.CT_Say, eChatLoc.CL_ChatWindow);
                OfferTraining(player);
            }
            else
            {
                // perhaps player can be promoted
                if (CanPromotePlayer(player))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.Interact.Text1", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    if (!player.IsLevelRespecUsed)
                    {
                        OfferRespecialize(player);
                    }
                }
                else
                {
                    CheckChampionTraining(player);
                }
            }
            return true;
        }

        /// <summary>
        /// Talk to trainer
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text)) return false;
            GamePlayer player = source as GamePlayer;

            if (CanPromotePlayer(player))
            {
                switch (text)
                {
                    // Mauler_Hib = 62
                    case "Temple of the Iron Fist":
                    case "Temple de la Main de Fer":
                        player!.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;

                    case "staff":
                    case "bâton":
                        PromotePlayer(player, (int)eCharacterClass.MaulerHib, LanguageMgr.GetTranslation(player!.Client.Account.Language, "MaulerHibTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, WEAPON_ID1, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, ARMOR_ID1, eInventoryActionType.Other);
                        break;

                    case "fists":
                    case "poings":
                        PromotePlayer(player, (int)eCharacterClass.MaulerHib, LanguageMgr.GetTranslation(player!.Client.Account.Language, "MaulerHibTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, WEAPON_ID2, eInventoryActionType.Other);
                        player.ReceiveItem(this, WEAPON_ID2, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, ARMOR_ID1, eInventoryActionType.Other);
                        break;
                }
            }
            return true;
        }

        /// <summary>
        /// For Recieving Armors.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            if (source == null || item == null) return false;

            GamePlayer player = source as GamePlayer;

            if (player!.Level >= 10 && player.Level < 15 && item.Id_nb == ARMOR_ID1)
            {
                player.Inventory.RemoveCountFromStack(item, 1);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                addGift(ARMOR_ID2, player);
            }
            if (player.Level >= 15 && player.Level < 20 && item.Id_nb == ARMOR_ID2)
            {
                player.Inventory.RemoveCountFromStack(item, 1);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                addGift(ARMOR_ID3, player);
            }
            if (player.Level >= 20 && player.Level < 50 && item.Id_nb == ARMOR_ID3)
            {
                player.Inventory.RemoveCountFromStack(item, 1);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                addGift(ARMOR_ID4, player);
            }
            return base.ReceiveItem(source, item);
        }
    }
}
