using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public abstract class CustomVault : GameHouseVault
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const int SIZE = 100;
        public const int Last_Used_FIRST_SLOT = 1600;
        public const int FIRST_SLOT = 2500;
        private readonly string m_vaultOwner;

        private readonly object _vaultLock = new object();

        /// <summary>
        /// A custom vault that masquerades as a house vault to the game client
        /// </summary>
        /// <param name="player">Player who owns the vault</param>
        /// <param name="vaultNPC">NPC controlling the interaction between player and vault</param>
        /// <param name="vaultOwner">ID of vault owner (can be anything unique, if it's the account name then all toons on account can access the items)</param>
        /// <param name="vaultNumber">Valid vault IDs are 0-3</param>
        /// <param name="dummyTemplate">An ItemTemplate to satisfy the base class's constructor</param>
        public CustomVault(GamePlayer player, string vaultOwner, int vaultNumber, ItemTemplate dummyTemplate)
            : base(dummyTemplate, vaultNumber)
        {
            m_vaultOwner = vaultOwner;

            DBHouse dbh = new DBHouse();
            dbh.AllowAdd = false;
            dbh.GuildHouse = false;
            dbh.HouseNumber = player.ObjectID;
            dbh.Name = "Vault";
            dbh.OwnerID = vaultOwner;
            dbh.RegionID = player.CurrentRegionID;
            CurrentHouse = new House(dbh);
        }

        public override bool Interact(GamePlayer player)
        {
            if (!CanView(player))
            {
                player.Out.SendMessage("You don't have permission to view this vault!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (player.ActiveInventoryObject != null)
            {
                player.ActiveInventoryObject.RemoveObserver(player);
            }

            AddObserver(player);

            player.ActiveInventoryObject = this;
            player.Out.SendInventoryItemsUpdate(GetClientInventory(player), eInventoryWindowType.HouseVault);
            return true;
        }

        protected void AddObserver(GamePlayer player)
        {
            lock (_vaultLock)
            {
                _observers.TryAdd(player.Name, player);
            }
        }

        public override Dictionary<int, InventoryItem> GetClientInventory(GamePlayer player)
        {
            var items = new Dictionary<int, InventoryItem>();
            int slotOffset = -FirstDBSlot + (int)(eInventorySlot.HousingInventory_First);
            foreach (InventoryItem item in DBItems(player))
            {
                if (item != null)
                {
                    if (!items.ContainsKey(item.SlotPosition + slotOffset))
                    {
                        items.Add(item.SlotPosition + slotOffset, item);
                    }
                    else
                    {
                       log.ErrorFormat("GAMECUSTOMVAULT: Duplicate item {0}, owner {1}, position {2}", item.Name, item.OwnerID, (item.SlotPosition + slotOffset));
                    }
                }
            }

            return items;
        }

        /// <summary>
        /// Move an item from, to or inside a house vault.  From IGameInventoryObject
        /// </summary>
        public override bool MoveItem(GamePlayer player, ushort fromSlot, ushort toSlot, ushort count)
        {
            if (fromSlot == toSlot)
            {
                return false;
            }

            bool fromVault = IsVaultInventorySlot(fromSlot);
            bool toVault = IsVaultInventorySlot(toSlot);

            if (fromVault == false && toVault == false)
            {
                return false;
            }

            //Prevent exploit shift+clicking quiver exploit
            if (fromVault)
            {
                if (fromSlot < (ushort)eInventorySlot.HousingInventory_First || fromSlot > (ushort) eInventorySlot.HousingInventory_Last) return false;
            }

            GameVault gameVault = player.ActiveInventoryObject as GameVault;
            if (gameVault == null)
            {
                player.Out.SendMessage("You are not actively viewing a vault!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            InventoryItem itemInToSlot = null;
            InventoryItem itemInFromSlot = null;

            if (toVault)
            {
                itemInToSlot = player.Inventory.GetItem((eInventorySlot)toSlot);
                if (itemInToSlot != null)
                {
                    if (!gameVault.CanRemoveItem(player, itemInToSlot))
                    {
                        player.Out.SendMessage("You don't have permission to remove this item!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return false;
                    }
                }
                if (!gameVault.CanAddItem(player, player.Inventory.GetItem((eInventorySlot)fromSlot)))
                {
                    player.Out.SendMessage("You don't have permission to add items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            if (fromVault)
            {
                itemInFromSlot = player.Inventory.GetItem((eInventorySlot)fromSlot);
                if (!gameVault.CanRemoveItem(player, player.Inventory.GetItem((eInventorySlot)fromSlot)))
                {
                    player.Out.SendMessage("You don't have permission to remove items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            // Check for a swap to get around not allowing non-tradables in a housing vault - Tolakram
            if (fromVault && itemInToSlot is { IsTradable: false })
            {
                player.Out.SendMessage("You cannot swap with an untradable item!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                //log.DebugFormat("GameVault: {0} attempted to swap untradable item {2} with {1}", player.Name, itemInFromSlot.Name, itemInToSlot.Name);
                return false;
            }

            // Allow people to get untradables out of their house vaults (old bug) but 
            // block placing untradables into housing vaults from any source - Tolakram
            if (toVault && itemInFromSlot is { IsTradable: false })
            {
                /* DOL: if (itemInFromSlot.Id_nb != ServerProperties.Properties.ALT_CURRENCY_ID) */
                {
                    player.Out.SendMessage("You can not put this item into a Vault!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            lock (m_vaultSync)
            {
                this.NotifyPlayers(this, player, _observers, this.MoveItem(player, (eInventorySlot) fromSlot, (eInventorySlot) toSlot, count));
            }

            return true;
        }

        /// <summary>
        /// Whether or not this player can view the contents of this vault.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool CanView(GamePlayer player)
        {
            if (GetOwner(player) == m_vaultOwner)
                return true;

            return false;
        }

        /// <summary>
        /// Whether or not this player can move items inside the vault
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool CanAddItem(GamePlayer player, InventoryItem item)
        {
            if (GetOwner(player) == m_vaultOwner)
                return true;

            return false;
        }

        /// <summary>
        /// Whether or not this player can move items inside the vault
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool CanRemoveItem(GamePlayer player, InventoryItem item)
        {
            if (GetOwner(player) == m_vaultOwner)
                return true;

            return false;
        }
    }
}