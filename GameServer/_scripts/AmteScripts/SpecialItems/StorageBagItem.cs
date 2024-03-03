using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Scripts
{
    public class StorageBagItem : GameInventoryItem
    {
        public StorageBagItem()
            : base()
        {
        }

        public StorageBagItem(ItemTemplate template)
            : base(template)
        {
        }

        public StorageBagItem(InventoryItem item)
            : base(item)
        {
            OwnerID = item.OwnerID;
            ObjectId = item.ObjectId;
        }

        public StorageBagVault BagVault { get; protected set; }

        /// <summary>
        /// Whether a bag has the ability to hold the item at any point (not counting e.g. current bag space)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual bool CanHoldItem(InventoryItem item)
        {
            return true;
        }

        /// <inheritdoc />
        public override bool Use(GamePlayer player)
        {
            StorageBagVault vault = new StorageBagVault(player, this);
            player.ActiveInventoryObject = vault;
            player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), eInventoryWindowType.PlayerVault);
            return (true);
        }

        /// <inheritdoc />
        public override void OnReceive(GamePlayer player)
        {
            BagVault = new StorageBagVault(player, this);

            base.OnReceive(player);
        }

        /// <inheritdoc />
        public override void OnLose(GamePlayer player)
        {
            BagVault = null;

            base.OnLose(player);
        }
    }

    public class IngredientsBag : StorageBagItem
    {
        public IngredientsBag()
            : base()
        {
        }

        public IngredientsBag(ItemTemplate template)
            : base(template)
        {
        }

        public IngredientsBag(InventoryItem item)
            : base(item)
        {
            OwnerID = item.OwnerID;
            ObjectId = item.ObjectId;
        }

        /// <inheritdoc />
        /*public override void OnReceive(GamePlayer player)
        {
            base.OnReceive(player);

            GameEventMgr.AddHandler(player, GamePlayerEvent.ReceiveItem, PlayerReceivesItem);
        }

        /// <inheritdoc />
        public override void OnLose(GamePlayer player)
        {
            base.OnLose(player);

            GameEventMgr.RemoveHandler(player, GamePlayerEvent.ReceiveItem, PlayerReceivesItem);
        }

        protected void PlayerReceivesItem(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is not GamePlayer player || args is not ReceiveItemEventArgs eventArgs)
                return;

            if (CanHoldItem(eventArgs.Item))
            {
                var allItems = BagVault.GetClientInventory(player);
                if (allItems.Count >= BagVault.VaultSize)
                {
                    return;
                }
                for (int slot = BagVault.FirstClientSlot; slot < BagVault.LastClientSlot; ++slot)
                {
                    if (!allItems.ContainsKey(slot))
                    {
                        BagVault.DoMoveItem(player, eventArgs.Item.SlotPosition, );
                    }
                }
            }
        }*/

        /// <inheritdoc />
        public override bool CanHoldItem(InventoryItem item)
        {
            if (item == null)
                return false;

            if (item.Item_Type != 40)
                return false;

            if (item.Object_Type is not ( 0 or 41 ))
                return false;

            if (item.PackageID is not ("craft_realm_update" or "craft_ingredient"))
                return false;

            return true;
        }
    }

    public class StorageBagVault : GameVault
    {
        private StorageBagItem BagItem { get; init; }

        public StorageBagVault(GamePlayer player, StorageBagItem item)
        {
            Name = item.Name;
            BagItem = item;
        }

        public override bool Interact(GamePlayer player)
        {
            if (player.ActiveInventoryObject != null)
            {
                player.ActiveInventoryObject.RemoveObserver(player);
            }

            AddObserver(player);

            player.ActiveInventoryObject = this;
            player.Out.SendInventoryItemsUpdate(GetClientInventory(player), eInventoryWindowType.HouseVault);
            return true;
        }

        /// <inheritdoc />
        public override bool CanAddItem(GamePlayer player, InventoryItem item)
        {
            return BagItem.CanHoldItem(item);
        }

        public override bool CanHandleMove(GamePlayer player, ushort fromSlot, ushort toSlot)
        {
            if (player == null || player.ActiveInventoryObject != this)
                return false;

            if (fromSlot >= (ushort)eInventorySlot.FirstVault && fromSlot <= (ushort)eInventorySlot.LastVault)
                return true;

            if (toSlot >= (ushort)eInventorySlot.FirstVault && toSlot <= (ushort)eInventorySlot.LastVault)
                return true;

            return false;
        }

        public override bool MoveItem(GamePlayer player, ushort fromSlot, ushort toSlot, ushort count)
        {
            if (fromSlot == toSlot)
            {
                return false;
            }

            bool fromVault = (fromSlot >= (ushort)eInventorySlot.FirstVault && fromSlot <= (ushort)eInventorySlot.LastVault);
            bool toVault = (toSlot >= (ushort)eInventorySlot.FirstVault && toSlot <= (ushort)eInventorySlot.LastVault);

            if (fromVault == false && toVault == false)
            {
                return false;
            }

            //Prevent exploit shift+clicking quiver exploit
            if (fromVault)
            {
                if (fromSlot < (ushort)eInventorySlot.HousingInventory_First || fromSlot > (ushort)eInventorySlot.HousingInventory_Last) return false;
            }

            StorageBagVault gameVault = player.ActiveInventoryObject as StorageBagVault;
            if (gameVault == null)
            {
                player.Out.SendMessage("You are not actively viewing a bag!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                player.Out.SendInventoryItemsUpdate(null);
                return false;
            }

            if (toVault)
            {
                if (!gameVault.CanAddItem(player, player.Inventory.GetItem((eInventorySlot)toSlot)))
                {
                    player.Out.SendMessage("You cannot put this item in this bag!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            DoMoveItem(player, fromSlot, toSlot, count);

            return true;
        }

        public void DoMoveItem(GamePlayer player, ushort fromSlot, ushort toSlot, ushort count)
        {

            lock (m_vaultSync)
            {
                this.NotifyPlayers(this, player, _observers, this.MoveItem(player, (eInventorySlot)fromSlot, (eInventorySlot)toSlot, count));
            }
        }

        /// <summary>
        /// List of items in the vault.
        /// </summary>
        public override IList<InventoryItem> DBItems(GamePlayer player = null)
        {
            return GameServer.Database.SelectObjects<InventoryItem>(DB.Column("OwnerID").IsEqualTo(BagItem.Id_nb).And(DB.Column("SlotPosition").IsGreaterOrEqualTo(FirstDBSlot).And(DB.Column("SlotPosition").IsLessOrEqualTo(LastDBSlot))));
        }
    }
}
