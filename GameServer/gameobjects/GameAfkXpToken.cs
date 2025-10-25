using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    /// <summary>
    /// AFK XP Token:
    /// - Each tick consumes 1 point of Condition.
    /// - When Condition reaches 0 the item is removed.
    /// - MaxCharges encodes the XP % per tick as hundredths of a percent:
    ///     1 => 0.01%, 100 => 1%, 9900 => 99%, etc.
    /// </summary>
    public class AfkXpToken : GameInventoryItem
    {
        public AfkXpToken() : base() { }
        public AfkXpToken(ItemTemplate t) : base(t) { }
        public AfkXpToken(InventoryItem it) : base(it) { }

        /// <summary>Percent of a level per tick, as 0.01 * MaxCharges.</summary>
        public double PercentOfLevelPerTick => MaxCharges / 100.0;

        /// <summary>Consume 1 condition; returns true if the token is still usable after consumption.</summary>
        public bool ConsumeOneCondition(GamePlayer owner)
        {
            if (Condition > 0) Condition -= 1;

            if (Condition <= 0)
            {
                if (owner?.Inventory != null)
                {
                    owner.Inventory.RemoveItem(this);
                    owner.Out.SendMessage(LanguageMgr.GetTranslation(owner, "Items.Specialitems.AfkXpToken.DepletedRemoved", Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                }
                return false;
            }

            owner?.Out.SendInventoryItemsUpdate(new InventoryItem[] { this });
            return true;
        }

        public static AfkXpToken FindOn(GamePlayer p)
        {
            if (p?.Inventory == null) return null;
            var items = p.Inventory.GetItemRange(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
            foreach (var it in items)
            {
                if (it is AfkXpToken token)
                    return token;
            }
            return null;
        }
    }
}
