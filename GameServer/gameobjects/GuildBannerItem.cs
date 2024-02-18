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
using DOL.AI.Brain;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using DOL.Events;
using DOL.Language;
using DOL.GS.PacketHandler;
using DOL.Database;
using DOL.GS.Spells;
using log4net;
using System.Linq;
using System.Numerics;

namespace DOL.GS
{
    /// <summary>
    /// This class represents an inventory item
    /// </summary>
    public class GuildBannerItem : GameInventoryItem
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public enum eStatus : byte
        {
            Active = 1,
            Dropped = 2,
            Recovered = 3
        }


        private eStatus m_status = eStatus.Active;

        public GuildBannerItem()
            : base()
        {
        }

        public GuildBannerItem(ItemTemplate template)
            : base(template)
        {
        }

        public GuildBannerItem(InventoryItem item)
            : base(item)
        {
            OwnerID = item.OwnerID;
            ObjectId = item.ObjectId;
        }

        public GuildBanner Banner { get; set; }

        public List<GamePlayer> StealingPlayers { get; set; } = new List<GamePlayer>();

        public WorldInventoryItem WorldItem { get; private set; }

        public eStatus Status { get; private set; }


        /// <summary>
        /// Player receives this item (added to players inventory)
        /// </summary>
        /// <param name="player"></param>
        public override void OnReceive(GamePlayer player)
        {
            /* VANILLA:
            // for guild banners we don't actually add it to inventory but instead register
            // if it is rescued by a friendly player or taken by the enemy

            player.Inventory.RemoveItem(this);

            int trophyModel = 0;
            eRealm realm = eRealm.None;

            switch (Model)
            {
                case 3223:
                    trophyModel = 3359;
                    realm = eRealm.Albion;
                    break;
                case 3224:
                    trophyModel = 3361;
                    realm = eRealm.Midgard;
                    break;
                case 3225:
                    trophyModel = 3360;
                    realm = eRealm.Hibernia;
                    break;
            }

            // if picked up by an enemy then turn this into a trophy
            if (realm != player.Realm)
            {
                ItemUnique template = new ItemUnique(Template);
                template.ClassType = "";
                template.Model = trophyModel;
                template.IsDropable = true;
                template.IsIndestructible = false;

                GameServer.Database.AddObject(template);
                GameInventoryItem trophy = new GameInventoryItem(template);
                player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, trophy);
                OwnerGuild.SendMessageToGuildMembers(player.Name + " of " + GlobalConstants.RealmToName(player.Realm) + " has captured your guild banner!", eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                OwnerGuild.GuildBannerLostTime = DateTime.Now;
            }
            else
            {
                m_status = eStatus.Recovered;

                // A friendly player has picked up the banner.
                if (OwnerGuild != null)
                {
                    OwnerGuild.SendMessageToGuildMembers(player.Name + " has recovered your guild banner!", eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                }

                if (SummonPlayer != null)
                {
                    SummonPlayer.GuildBanner = null;
                }
            }
            */

            // GONDWANA:
            if (Banner == null)
            {
                if (log.IsWarnEnabled)
                {
                    log.WarnFormat("Guild banner item {0} received by {1} has no guild banner object", Id_nb, player.Name);
                }
                return;
            }

            if (player.Group != null && player.Group == Banner.OwningPlayer?.Group) // Same group as owner, different or same guild, transfer to player
            {
                m_status = eStatus.Recovered;
                Banner.Guild.SendMessageToGuildMembers(player.Name + " has recovered your guild banner!", eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                player.Group.SendMessageToGroupMembers(player.Name + " has recovered your guild banner!", eChatType.CT_Group, eChatLoc.CL_SystemWindow);
                Banner.OwningPlayer = player;
                StealingPlayers.Clear();
                return;
            }

            player.Inventory.RemoveItem(this);
            if (StealingPlayers.Contains(player)) // Picked up by killers, turn into a trophy
            {
                GiveTrophy(player);
            }
            else if (player.Guild == Banner.Guild) // Same guild as owner, different group
            {
                player.Inventory.RemoveItem(this);
                m_status = eStatus.Recovered;
                Banner.Guild.SendMessageToGuildMembers(player.Name + " has recovered your guild banner!", eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            }
            else if (GameServer.ServerRules.IsAllowedToAttack(player, Banner.OwningPlayer, true)) // Enemy
            {
                GiveTrophy(player);
            }
            else // Friend
            {
                player.Inventory.RemoveItem(this);
                m_status = eStatus.Recovered;
                Banner.Guild.SendMessageToGuildMembers(player.Name + " has recovered your guild banner!", eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            }
        }

        protected void GiveTrophy(GamePlayer player)
        {
            int trophyModel = Model switch
            {
                3223 => 3359, // Albion
                3224 => 3361, // Midgard
                3225 => 3360, // Hibernia
                _ => 0
            };
            player.Inventory.RemoveItem(this);
            ItemUnique template = new ItemUnique(Template);
            template.ClassType = "";
            template.Model = trophyModel;
            template.IsDropable = true;
            template.IsIndestructible = false;

            GameServer.Database.AddObject(template);
            GameInventoryItem trophy = new GameInventoryItem(template);
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, trophy);
            Banner.Guild.SendMessageToGuildMembers(player.Name + " of " + player.Guild.Name + " has captured your guild banner!", eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            Banner.Guild.GuildBannerLostTime = DateTime.Now;
        }

        public WorldInventoryItem CreateWorldItem()
        {
            WorldInventoryItem item = new WorldInventoryItem(this);
            GamePlayer player = Banner.OwningPlayer;

            var point = player.GetPointFromHeading(player.Heading, 30);
            item.Position = new Vector3(point, player.Position.Z);
            item.Heading = player.Heading;
            item.CurrentRegionID = player.CurrentRegionID;
            return item;
        }

        /// <summary>
        /// Player has dropped, traded, or otherwise lost this item
        /// </summary>
        /// <param name="player"></param>
        public override void OnLose(GamePlayer player)
        {
            if (player.GuildBanner != null)
            {
                player.Guild?.SendMessageToGuildMembers(player.Name + " has dropped the guild banner!", eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                player.Group?.SendMessageToGroupMembers(player.Name + " has dropped the guild banner!", eChatType.CT_Group, eChatLoc.CL_SystemWindow);
                player.GuildBanner.Stop();
                m_status = eStatus.Dropped;
            }
        }

        /// <summary>
        /// Player has dropped, traded, or otherwise lost this item
        /// </summary>
        /// <param name="player"></param>
        public void OnPlayerKilled(GameObject killer)
        {
            WorldItem = CreateWorldItem();
            if (killer is GameLiving { Group: not null } livingKiller) // Player or NPC in a group
            {
                StealingPlayers = new List<GamePlayer>(livingKiller.Group.GetPlayersInTheGroup());
            }
            else // Killer not in a group
            {
                if (killer is GamePlayer playerKiller)
                {
                    StealingPlayers = new List<GamePlayer>
                    {
                        playerKiller
                    };
                }
                else if (killer is GameNPC { Brain: IControlledBrain { Owner: GamePlayer } } pet)
                {
                    StealingPlayers = new List<GamePlayer>
                    {
                        (GamePlayer)((IControlledBrain)pet.Brain).Owner
                    };
                }
            }

            if (StealingPlayers.Any()) // Only restrict pickups if killed by player
            {
                if (Banner.OwningPlayer.Group != null)
                {
                    foreach (GamePlayer owningPlayer in Banner.OwningPlayer.Group.GetPlayersInTheGroup())
                    {
                        WorldItem.AddOwner(owningPlayer);
                    }
                }
                else
                {
                    WorldItem.AddOwner(Banner.OwningPlayer);
                }

                foreach (GamePlayer stealingPlayer in StealingPlayers)
                {
                    WorldItem.AddOwner(stealingPlayer);
                }
            }

            WorldItem.StartPickupTimer(10);
            OnLose(Banner.OwningPlayer);
            WorldItem.AddToWorld();
        }

        /// <summary>
        /// Drop this item on the ground
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override WorldInventoryItem Drop(GamePlayer player)
        {
            return null;
        }


        public override void OnRemoveFromWorld()
        {
            if (Banner is { Guild: not null })
            {
                if (Status == eStatus.Dropped)
                {
                    // banner was dropped and not picked up, must be re-purchased
                    Banner.Guild.GuildBanner = false;
                    Banner.Guild.SendMessageToGuildMembers("Your guild banner has been lost!", eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    Banner.Guild = null;
                    Banner.OwningPlayer = null;
                }
            }

            base.OnRemoveFromWorld();
        }


        /// <summary>
        /// Is this a valid item for this player?
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool CheckValid(GamePlayer player)
        {
            return false;
        }
    }
}
