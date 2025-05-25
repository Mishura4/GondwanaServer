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
        public class DepositedItem
        {
            public string Id_nb { get; set; } = string.Empty;
            public string ItemName { get; set; } = string.Empty;
            public int Count { get; set; } = 0;
            public int PointsPerItem { get; set; } = 0;
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

        private readonly List<DepositedItem> m_depositedItems = new();

        public IReadOnlyList<DepositedItem> DepositedItems
        {
            get
            {
                lock (m_depositedItems)
                {
                    return m_depositedItems.ToImmutableList();
                }
            }
        }
        
        public PVPChest(PvPScore score)
        {
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

            int totalDeposited = 0;

            // For each item in backpack, if it's a PvPTreasure, remove & deposit
            for (eInventorySlot slot = eInventorySlot.FirstBackpack;
                 slot <= eInventorySlot.LastBackpack; slot++)
            {
                InventoryItem item = player.Inventory.GetItem(slot);
                if (item is PvPTreasure treasure)
                {
                    if (player.Inventory.RemoveItem(item))
                    {
                        AddDepositedItem(treasure);
                        totalDeposited += treasure.Count;
                    }
                }
            }

            if (totalDeposited > 0)
            {
                player.Out.SendMessage($"You have deposited {totalDeposited} treasure item(s).", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                RecalcChestScore();
            }
            else
            {
                player.Out.SendMessage("No treasure items were deposited.",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        #endregion

        #region Internal deposit data + scoreboard

        private void AddDepositedItem(PvPTreasure treasure)
        {
            var existing = m_depositedItems.FirstOrDefault(di =>
                di.Id_nb == treasure.Id_nb && di.ItemName == treasure.Name);
            if (existing != null)
            {
                existing.Count += treasure.Count;
            }
            else
            {
                m_depositedItems.Add(new DepositedItem
                {
                    Id_nb = treasure.Id_nb,
                    ItemName = treasure.Name,
                    Count = treasure.Count,
                    PointsPerItem = treasure.TreasurePoints
                });
            }
        }

        /// <summary>
        /// Calculate how many points are in the chest. If SessionType=3 and
        /// the scoreboard owner is in PvP, sets .Treasure_BroughtTreasuresPoints
        /// to that total.
        /// If the chest is “unlocked” (owner not in PvP, or null), no effect on scoreboard.
        /// </summary>
        private void RecalcChestScore()
        {
            var pm = PvpManager.Instance;
            if (pm == null || !pm.IsOpen) return;
            if (pm.CurrentSessionType is not PvpManager.eSessionTypes.TreasureHunt) return;

            if (Unlocked || m_score is null) return; // chest is unlocked => no scoreboard update

            int totalPoints = 0;
            foreach (var di in m_depositedItems)
            {
                totalPoints += di.Count * di.PointsPerItem;
            }

            //sbPlayer.Out.SendMessage($"[Chest Score] Your chest has {totalPoints} total treasure points.",
            //    eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        #endregion

        #region Stealing logic

        /// <summary>
        /// Removes one random item from the chest. If none remain, returns null.
        /// </summary>
        public DepositedItem StealRandomItem()
        {
            if (m_depositedItems.Count == 0)
                return null;

            var rnd = new Random();
            int index = rnd.Next(m_depositedItems.Count);
            var di = m_depositedItems[index];

            di.Count--;
            if (di.Count <= 0)
                m_depositedItems.RemoveAt(index);

            return new DepositedItem
            {
                Id_nb = di.Id_nb,
                ItemName = di.ItemName,
                Count = 1,
                PointsPerItem = di.PointsPerItem
            };
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
            if (stolenData == null)
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

            // Recalc the chest's total deposit for them
            RecalcChestScore();
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
                foreach (var di in m_depositedItems)
                {
                    lines.Add($"{i}. {di.ItemName} [Id_nb={di.Id_nb}] x{di.Count}, {di.PointsPerItem} pts each");
                    i++;
                }
            }

            return lines;
        }

        #endregion
    }
}
