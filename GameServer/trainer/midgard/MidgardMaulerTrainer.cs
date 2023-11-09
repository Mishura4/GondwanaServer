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
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.Database;

namespace DOL.GS.Trainer
{
    /// <summary>
    /// Mauler Trainer
    /// </summary>
    [NPCGuildScript("Mauler Trainer", eRealm.Midgard)]
    public class MidgardMaulerTrainer : GameTrainer
    {
        public override eCharacterClass TrainedClass
        {
            get { return eCharacterClass.MaulerMid; }
        }

        public const string WEAPON_ID1 = "maulermid_item_staff";
        public const string WEAPON_ID2 = "maulermid_item_fist";

        /// <summary>
        /// Interact with trainer
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;

            // check if class matches.
            if (player.CharacterClass.ID == (int)TrainedClass)
            {
                OfferTraining(player);
            }
            else
            {
                // perhaps player can be promoted
                if (CanPromotePlayer(player))
                {
                    player.Out.SendMessage(this.Name + " says, \"Do you desire to [join the Temple of the Iron Fist] and fight for the glorious realm of Midgard?\"", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
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
                    // Mauler_mid = 61
                    case "Temple of the Iron Fist":
                    case "Temple du Poing de Fer":
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerMidTrainer.WhisperReceive.Text1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;

                    case "staff":
                        PromotePlayer(player, (int)eCharacterClass.MaulerMid, LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerMidTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, WEAPON_ID1, eInventoryActionType.Other);
                        break;

                    case "fist":
                        PromotePlayer(player, (int)eCharacterClass.MaulerMid, LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerMidTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, WEAPON_ID2, eInventoryActionType.Other);
                        player.ReceiveItem(this, WEAPON_ID2, eInventoryActionType.Other);
                        break;
                }
            }
            return true;
        }
    }
}
