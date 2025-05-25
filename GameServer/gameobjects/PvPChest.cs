#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AmteScripts.Managers;
using AmteScripts.PvP;
using AmteScripts.PvP.CTF; // adjust if your manager namespace is different
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace DOL.GS
{
    /// <summary>
    /// A PvPChest that can be owned either by a single player (solo)
    /// or by an entire group (owner = group leader). The scoreboard
    /// is updated for that “owner” player, but if the owner is not in
    /// PvP or no owner is set, it becomes “unlocked” for everyone.
    /// </summary>
    public class PVPChest : GamePvPStaticItem
    {
        /// <summary>
        /// For storing the item deposits in this chest.
        /// </summary>
        public record DepositedItem(string Id_nb, int PointsPerItem)
        {
            public int Count { get; set; } = 0;

            public string ItemName { get; set; } = string.Empty;
            
            public DepositedItem(PvPTreasure treasure) : this(treasure.Id_nb!, treasure.TreasurePoints)
            {
                ItemName = treasure.Name!;
                Count = treasure.Count;
            }

            public bool IsSameItem(DepositedItem item)
            {
                return Equals(Id_nb, item.Id_nb) && Equals(ItemName, item.ItemName) && Equals(PointsPerItem, item.PointsPerItem);
            }

            public void Merge(DepositedItem item)
            {
                Debug.Assert(IsSameItem(item));

                this.Count += item.Count;
                if (string.IsNullOrEmpty(ItemName))
                    ItemName = item.ItemName;
                item.Count = 0;
            }

            public DepositedItem Split(int count = 1)
            {
                count = Math.Max(this.Count, count);
                this.Count -= count;
                return this with { Count = count };
            }
        }

        // ============ Ownership Info ============

        /// <summary>
        /// True if we are group-owned, false if solo-owned.
        /// </summary>
        public bool IsGroupChest => OwnerGuild != null;

        public bool Unlocked
        {
            get;
            set;
        }

        // ============ Chest Data ============

        private PvPScore? m_score = null;

        private readonly ReaderWriterDictionary<string, DepositedItem> m_depositedItems = new();

        public IReadOnlyDictionary<string, DepositedItem> DepositedItems
        {
            get
            {
                return m_depositedItems.ToImmutableDictionary();
            }
        }
        
        public PVPChest(PvPScore score)
        {
            m_score = score;
            Name = "PvP Chest";
        }

        #region Ownership Setup

        /// <summary>
        /// Checks if the 'player' is allowed to deposit into this chest,
        /// or if the chest is "unlocked" (no owner or owner not in PvP).
        /// 
        /// If there's a valid scoreboard owner who is in PvP, we enforce
        /// that only that owner (or that group) can deposit.
        /// 
        /// If the scoreboard owner is NOT in PvP (or is null),
        /// then the chest is effectively open to everyone.
        /// </summary>
        /// <inheritdoc />
        public override bool IsOwner(GameLiving other)
        {
            if (Unlocked)
                return true; // unlocked => anyone can deposit
            
            return base.IsOwner(other);
        }

        #endregion

        #region Interact + deposit flow

        public override bool Interact(GamePlayer player)
        {
            if (player == null) return false;

            if (m_score is null)
            {
                player.Out.SendMessage("This chest cannot be deposited into!",
                                       eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            // If not the owner or chest unlocked => check
            if (!IsOwner(player))
            {
                player.Out.SendMessage("You do not own this chest!",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            // Check if player has any PvPTreasure in backpack
            bool hasTreasure = false;
            for (eInventorySlot slot = eInventorySlot.FirstBackpack;
                 slot <= eInventorySlot.LastBackpack; slot++)
            {
                InventoryItem item = player.Inventory.GetItem(slot);
                if (item is PvPTreasure)
                {
                    hasTreasure = true;
                    break;
                }
            }
            if (!hasTreasure)
            {
                player.Out.SendMessage("You have no treasure to deposit!",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            // Confirm deposit
            player.TempProperties.setProperty("PVPChest_Interact", this);
            player.Out.SendCustomDialog(
                "Deposit ALL treasure items from your backpack into this PvP Chest?",
                new CustomDialogResponse(DepositResponseCallback)
            );
            return true;
        }

        private void DepositResponseCallback(GamePlayer player, byte response)
        {
            player.TempProperties.removeProperty("PVPChest_Interact");
            if (response != 0x01)
            {
                player.Out.SendMessage("Deposit cancelled.",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            int depositedCount = 0;

            // For each item in backpack, if it's a PvPTreasure, remove & deposit
            for (eInventorySlot slot = eInventorySlot.FirstBackpack;
                 slot <= eInventorySlot.LastBackpack; slot++)
            {
                InventoryItem item = player.Inventory.GetItem(slot);
                if (item is PvPTreasure treasure)
                {
                    if (player.Inventory.RemoveItem(item))
                    {
                        DepositItem(player, treasure);
                        depositedCount += treasure.Count;
                    }
                }
            }

            if (depositedCount > 0)
            {
                player.Out.SendMessage($"You have deposited {depositedCount} treasure item(s).", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                SendScore();
            }
            else
            {
                player.Out.SendMessage("No treasure items were deposited.",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        #endregion

        #region Internal deposit data + scoreboard
        private int DepositItem(GamePlayer source, PvPTreasure treasure)
        {
            var toAdd = new DepositedItem(treasure);
            var (added, item) = m_depositedItems.AddIfNotExists(treasure.Id_nb, toAdd);
            if (!added)
            {
                item.Merge(toAdd);
            }

            if (m_score is null)
                return 0;

            var points = item.Count + item.PointsPerItem;
            m_score.Treasure_BroughtTreasuresPoints += points;
            if (m_score is PvPGroupScore groupScore)
            {
                groupScore.GetOrCreateScore(source).Treasure_BroughtTreasuresPoints += points;
            }
            return points;
        }

        private void SendScore(GamePlayer? playerTo = null)
        {
            if (m_score is null)
                return;
            
            if (playerTo == null)
            {
                if (IsGroupChest)
                {
                    foreach (GamePlayer player in OwnerGuild.GetListOfOnlineMembers())
                    {
                        SendScore(player);
                    }
                }
                else if (Owner is GamePlayer player)
                {
                    SendScore(player);
                }
                return;
            }

            var points = Math.Max(0, m_score.Treasure_BroughtTreasuresPoints - m_score.Treasure_StolenTreasuresPoints);
            playerTo.Out.SendMessage($"[Chest Score] Your chest has {points} total treasure points.",
                                     eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
        #endregion

        #region Stealing logic

        /// <summary>
        /// Removes one random item from the chest. If none remain, returns null.
        /// </summary>
        public DepositedItem? StealRandomItem()
        {
            if (m_depositedItems.Count == 0)
                return null;
            
            DepositedItem? stolen = null;
            var rnd = new Random();
            m_depositedItems.FreezeWhile(items =>
            {
                if (items.Count == 0)
                    return;
                
                int index = rnd.Next(items.Count);
                var di = items.ElementAt(index).Value;

                stolen = di.Split();
                if (di.Count <= 0)
                    items.Remove(di.Id_nb);
            });
            return stolen is not { Count: > 0 } ? null : stolen;
        }

        /// <summary>
        /// Called by external code if a stealer successfully steals from the chest.
        /// The chest “owner” is considered the victim for scoreboard. We add +5 
        /// to their .Treasure_StolenTreasuresPoints, then recalc.
        /// If chest is “unlocked,” it still uses the scoreboardOwner if in PvP.
        /// </summary>
        public void OnTreasureStolen(GamePlayer stealer, DepositedItem? stolenData)
        {
            // stolenData was returned by StealRandomItem() or some other logic
            if (stolenData is null)
            {
                stealer.Out.SendMessage("You tried to steal, but the chest is empty!",
                    eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
            }

            stealer.Out.SendMessage($"You stole a [{stolenData.ItemName}] from the chest!",
                eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            // only if we are in open session 3 and the chest is locked
            var pm = PvpManager.Instance;
            if (!pm.IsOpen || pm.CurrentSessionType is not PvpManager.eSessionTypes.TreasureHunt) return;
            if (!Unlocked || m_score == null) return;

            // The chest owner "loses" x points in .Treasure_StolenTreasuresPoints
            // We'll increment by stolenData.Count * stolenData.PointsPerItem
            // (which is typically 1 item * that item’s points).

            m_score.Treasure_StolenTreasuresPoints += 3;
            SendScore();
        }

        #endregion

        #region GM or Debug Info

        /// <summary>
        /// Shows info about chest ownership and items.
        /// You can call this from e.g. /wholoot or some GM command.
        /// </summary>
        public IList<string> DelveInfo()
        {
            var lines = new List<string>();

            if (m_score is null)
            {
                lines.Add("");
                lines.Add("Chest has no scoreboard owner (unlocked).");
                lines.Add("");
            }
            else
            {
                lines.Add("");
                if (!IsGroupChest)
                    lines.Add("Owned by a single player: " + m_score?.PlayerName);
                else
                    lines.Add("Group chest. Leader: " + m_score?.PlayerName);
                lines.Add(Unlocked ? " - Locked" : " - Unlocked");
                lines.Add("");
            }

            lines.Add($"Total distinct item types inside: {m_depositedItems.Count}");
            if (m_depositedItems.Count == 0)
            {
                lines.Add("  (Chest is empty)");
            }
            else
            {
                int i = 1;
                foreach (var entry in m_depositedItems)
                {
                    var di = entry.Value;
                    var name = string.IsNullOrEmpty(di.ItemName) ? di.Id_nb : di.ItemName;
                    lines.Add($"{i}. {name} [Id_nb={di.Id_nb}] x{di.Count}, {di.PointsPerItem} pts each");
                    i++;
                }
            }

            return lines;
        }

        #endregion
    }
}
