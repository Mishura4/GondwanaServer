using System;
using System.Collections.Generic;
using System.Linq;
using AmteScripts.Managers; // adjust if your manager namespace is different
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    /// <summary>
    /// A PvPChest that can be owned either by a single player (solo)
    /// or by an entire group (owner = group leader). The scoreboard
    /// is updated for that “owner” player, but if the owner is not in
    /// PvP or no owner is set, it becomes “unlocked” for everyone.
    /// </summary>
    public class PVPChest : GameStaticItem
    {
        /// <summary>
        /// For storing the item deposits in this chest.
        /// </summary>
        public class DepositedItem
        {
            public string Id_nb { get; set; }
            public string ItemName { get; set; }
            public int Count { get; set; }
            public int PointsPerItem { get; set; }
        }

        // ============ Ownership Info ============

        // If solo chest, we store the single player:
        private GamePlayer m_ownerPlayer;

        // If group chest, we store the group reference and the group leader:
        private Group m_ownerGroup;
        private GamePlayer m_ownerGroupLeader;

        /// <summary>
        /// True if we are group-owned, false if solo-owned.
        /// </summary>
        public bool IsGroupChest { get; private set; }

        // ============ Chest Data ============

        private List<DepositedItem> m_depositedItems = new List<DepositedItem>();

        public PVPChest()
        {
            Name = "PvP Chest";
        }

        #region Ownership Setup

        /// <summary>
        /// Called if you want this chest to belong to a single player (solo).
        /// </summary>
        public void SetOwnerSolo(GamePlayer player)
        {
            IsGroupChest = false;
            m_ownerPlayer = player;
            m_ownerGroup = null;
            m_ownerGroupLeader = null;
        }

        /// <summary>
        /// Called if you want this chest to belong to a group
        /// and have the scoreboard credited to that group's leader.
        /// </summary>
        public void SetOwnerGroup(GamePlayer groupLeader, Group group)
        {
            IsGroupChest = true;
            m_ownerGroupLeader = groupLeader;
            m_ownerGroup = group;
            m_ownerPlayer = null;
        }

        /// <summary>
        /// Returns whichever player's scoreboard should be updated
        /// for deposits/steals: solo owner if not group, or group leader if group.
        /// 
        /// WARNING: If the group’s leader changes, we update m_ownerGroupLeader 
        /// to the new leader automatically. So the scoreboard credit always 
        /// goes to the current group leader.
        /// </summary>
        public GamePlayer GetScoreboardPlayer()
        {
            if (!IsGroupChest)
            {
                // Solo chest => single player
                return m_ownerPlayer;
            }
            else
            {
                // Group chest => check if the group leader changed
                if (m_ownerGroup != null && m_ownerGroup.Leader is GamePlayer newLeader)
                {
                    m_ownerGroupLeader = newLeader;
                }
                return m_ownerGroupLeader;
            }
        }

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
        private bool IsOwnerOrUnlocked(GamePlayer player)
        {
            var scoreboardOwner = GetScoreboardPlayer();
            if (scoreboardOwner == null)
            {
                // No owner => open chest
                return true;
            }

            // If the scoreboard owner is no longer in PvP => chest is unlocked
            if (!scoreboardOwner.IsInPvP)
            {
                return true;
            }

            // Otherwise, enforce normal ownership
            if (!IsGroupChest)
            {
                // solo => must be exactly scoreboardOwner
                return (player == scoreboardOwner);
            }
            else
            {
                // group => check that player is in the same group as scoreboardOwner
                if (player.Group == null || scoreboardOwner.Group == null)
                    return false;

                return (player.Group == scoreboardOwner.Group);
            }
        }

        #endregion

        #region Interact + deposit flow

        public override bool Interact(GamePlayer player)
        {
            if (player == null) return false;

            // If not the owner or chest unlocked => check
            if (!IsOwnerOrUnlocked(player))
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
            if (pm.CurrentSession?.SessionType != 3) return;

            var sbPlayer = GetScoreboardPlayer();
            if (sbPlayer == null) return;
            if (!sbPlayer.IsInPvP) return; // chest is unlocked => no scoreboard update

            var score = pm.GetIndividualScore(sbPlayer);
            if (score == null) return;

            int totalPoints = 0;
            foreach (var di in m_depositedItems)
            {
                totalPoints += di.Count * di.PointsPerItem;
            }

            score.Treasure_BroughtTreasuresPoints = totalPoints;

            sbPlayer.Out.SendMessage($"[Chest Score] Your chest has {totalPoints} total treasure points.",
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
        public void OnTreasureStolen(GamePlayer stealer, DepositedItem stolenData)
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

            // only if we are in open session 3 and the owner is in pvp
            var pm = PvpManager.Instance;
            if (pm == null || !pm.IsOpen) return;
            if (pm.CurrentSession?.SessionType != 3) return;

            var sbPlayer = GetScoreboardPlayer();
            if (sbPlayer == null) return;
            if (!sbPlayer.IsInPvP) return;

            // The chest owner "loses" x points in .Treasure_StolenTreasuresPoints
            // We'll increment by stolenData.Count * stolenData.PointsPerItem
            // (which is typically 1 item * that item’s points).
            var victimScore = pm.GetIndividualScore(sbPlayer);
            if (victimScore == null) return;

            victimScore.Treasure_StolenTreasuresPoints += 3;

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
            var sbOwner = GetScoreboardPlayer();
            bool hasOwner = (sbOwner != null);

            if (!hasOwner)
            {
                lines.Add("");
                lines.Add("Chest has no scoreboard owner (unlocked).");
                lines.Add("");
            }
            else if (!IsGroupChest)
            {
                lines.Add("");
                lines.Add("Owned by a single player: " + sbOwner!.Name);
                lines.Add(sbOwner.IsInPvP
                    ? "Owner is in PvP => locked to that owner"
                    : "Owner is not in PvP => chest is unlocked");
                lines.Add("");
            }
            else
            {
                lines.Add("");
                lines.Add("Group chest. Leader: " + sbOwner!.Name);
                lines.Add(sbOwner.IsInPvP
                    ? "Leader is in PvP => locked to that group"
                    : "Leader not in PvP => chest is unlocked");
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
