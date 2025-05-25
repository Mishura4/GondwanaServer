#nullable enable
using DOL.GS;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static AmteScripts.Managers.PvpManager;

namespace AmteScripts.PvP
{
    public record PvPScore(bool IsSoloScore)
    {
        public PvPScore(string playerName, string playerID, bool isSoloScore) : this(isSoloScore)
        {
            PlayerName = playerName;
            PlayerID = playerID;
        }
        
        public PvPScore(GamePlayer player, bool isSoloScore) : this(player.Name!, player.InternalID!, isSoloScore)
        {
        }
        
        public PvPScore(Guild guild) : this(guild.Name!, guild.GuildID!, false)
        {
        }
        
        public string PlayerName { get; set; }
        public string PlayerID { get; set; }

        // --- For session type #1: PvP Combat ---
        [DefaultValue(0)]
        public int PvP_SoloKills { get; set; }
        [DefaultValue(0)]
        public int PvP_SoloKillsPoints { get; set; }
        [DefaultValue(0)]
        public int PvP_GroupKills { get; set; }
        [DefaultValue(0)]
        public int PvP_GroupKillsPoints { get; set; }

        // --- For session type #2: Flag Capture ---
        [DefaultValue(0)]
        public int Flag_SoloKills { get; set; }
        [DefaultValue(0)]
        public int Flag_SoloKillsPoints { get; set; }
        [DefaultValue(0)]
        public int Flag_GroupKills { get; set; }
        [DefaultValue(0)]
        public int Flag_GroupKillsPoints { get; set; }
        [DefaultValue(0)]
        public int Flag_KillFlagCarrierCount { get; set; }
        [DefaultValue(0)]
        public int Flag_KillFlagCarrierPoints { get; set; }
        [DefaultValue(0)]
        public int Flag_FlagReturnsCount { get; set; }
        [DefaultValue(0)]
        public int Flag_FlagReturnsPoints { get; set; }
        [DefaultValue(0)]
        public int Flag_OwnershipPoints { get; set; }

        // --- For session type #3: Treasure Hunt ---
        /// <summary>
        /// For storing the item deposits in this chest.
        /// </summary>
        public record Item(string Id_nb, int PointsPerItem)
        {
            public int Count { get; set; } = 0;

            public string ItemName { get; set; } = string.Empty;
            
            public Item(PvPTreasure treasure) : this(treasure.Id_nb!, treasure.TreasurePoints)
            {
                ItemName = treasure.Name!;
                Count = treasure.Count;
            }

            public bool IsSameItem(Item item)
            {
                return Equals(Id_nb, item.Id_nb) && Equals(ItemName, item.ItemName) && Equals(PointsPerItem, item.PointsPerItem);
            }

            public void Merge(Item item)
            {
                Debug.Assert(IsSameItem(item));

                this.Count += item.Count;
                if (string.IsNullOrEmpty(ItemName))
                    ItemName = item.ItemName;
                item.Count = 0;
            }

            public Item Split(int count = 1)
            {
                count = Math.Max(this.Count, count);
                this.Count -= count;
                return this with { Count = count };
            }
        }

        [DefaultValue(0)]
        public int Treasure_SoloKills { get; set; }
        [DefaultValue(0)]
        public int Treasure_SoloKillsPoints { get; set; }
        [DefaultValue(0)]
        public int Treasure_GroupKills { get; set; }
        [DefaultValue(0)]
        public int Treasure_GroupKillsPoints { get; set; }
        
        [IgnoreDataMember]
        public ReaderWriterDictionary<string, Item> Treasure_Items { get; init; } = new();
        
        public int Treasure_BroughtTreasuresPoints { get; set; }
        [DefaultValue(0)]
        public int Treasure_StolenTreasuresPoints { get; set; }

        // --- For session type #4: Bring Friends ---
        [DefaultValue(0)]
        public int Friends_SoloKills { get; set; }
        [DefaultValue(0)]
        public int Friends_SoloKillsPoints { get; set; }
        [DefaultValue(0)]
        public int Friends_GroupKills { get; set; }
        [DefaultValue(0)]
        public int Friends_GroupKillsPoints { get; set; }
        [DefaultValue(0)]
        public int Friends_BroughtFriendsPoints { get; set; }
        [DefaultValue(0)]
        public int Friends_BroughtFamilyBonus { get; set; }
        [DefaultValue(0)]
        public int Friends_FriendKilledCount { get; set; }
        [DefaultValue(0)]
        public int Friends_FriendKilledPoints { get; set; }
        [DefaultValue(0)]
        public int Friends_KillEnemyFriendCount { get; set; }
        [DefaultValue(0)]
        public int Friends_KillEnemyFriendPoints { get; set; }

        // --- For session type #5: Capture Territories ---
        [DefaultValue(0)]
        public int Terr_SoloKills { get; set; }
        [DefaultValue(0)]
        public int Terr_SoloKillsPoints { get; set; }
        [DefaultValue(0)]
        public int Terr_GroupKills { get; set; }
        [DefaultValue(0)]
        public int Terr_GroupKillsPoints { get; set; }
        [DefaultValue(0)]
        public int Terr_TerritoriesCapturedCount { get; set; }
        [DefaultValue(0)]
        public int Terr_TerritoriesCapturedPoints { get; set; }
        [DefaultValue(0)]
        public int Terr_TerritoriesOwnershipPoints { get; set; }

        // --- For session type #6: Boss Kill Cooperation ---
        [DefaultValue(0)]
        public int Boss_SoloKills { get; set; }
        [DefaultValue(0)]
        public int Boss_SoloKillsPoints { get; set; }
        [DefaultValue(0)]
        public int Boss_GroupKills { get; set; }
        [DefaultValue(0)]
        public int Boss_GroupKillsPoints { get; set; }
        [DefaultValue(0)]
        public int Boss_BossHitsCount { get; set; }
        [DefaultValue(0)]
        public int Boss_BossHitsPoints { get; set; }
        [DefaultValue(0)]
        public int Boss_BossKillsCount { get; set; }
        [DefaultValue(0)]
        public int Boss_BossKillsPoints { get; set; }

        public virtual void TakeItems(PvPScore rhs, bool copy = false)
        {
            Treasure_BroughtTreasuresPoints += rhs.Treasure_BroughtTreasuresPoints;
            Treasure_StolenTreasuresPoints += rhs.Treasure_StolenTreasuresPoints;
                
            List<KeyValuePair<string, PvPScore.Item>>? itemScore = null;
            if (copy)
            {
                rhs.Treasure_Items.HoldWhile(items =>
                {
                    itemScore = new(items);
                });
            }
            else
            {
                rhs.Treasure_Items.FreezeWhile(items =>
                {
                    rhs.Treasure_BroughtTreasuresPoints = 0;
                    rhs.Treasure_StolenTreasuresPoints = 0;
                    itemScore = new(items);
                    items.Clear();
                });
            }
            Treasure_Items.Add(itemScore!);
        }

        public virtual PvPScore Add(PvPScore rhs, bool transferItems = true, bool takeItems = true)
        {
            PvP_SoloKills += rhs.PvP_SoloKills;
            PvP_SoloKillsPoints += rhs.PvP_SoloKillsPoints;
            PvP_GroupKills += rhs.PvP_GroupKills;
            PvP_GroupKillsPoints += rhs.PvP_GroupKillsPoints;
            Flag_SoloKills += rhs.Flag_SoloKills;
            Flag_SoloKillsPoints += rhs.Flag_SoloKillsPoints;
            Flag_GroupKills += rhs.Flag_GroupKills;
            Flag_GroupKillsPoints += rhs.Flag_GroupKillsPoints;
            Flag_KillFlagCarrierCount += rhs.Flag_KillFlagCarrierCount;
            Flag_KillFlagCarrierPoints += rhs.Flag_KillFlagCarrierPoints;
            Flag_FlagReturnsCount += rhs.Flag_FlagReturnsCount;
            Flag_FlagReturnsPoints += rhs.Flag_FlagReturnsPoints;
            Flag_OwnershipPoints += rhs.Flag_OwnershipPoints;
            Treasure_SoloKills += rhs.Treasure_SoloKills;
            Treasure_SoloKillsPoints += rhs.Treasure_SoloKillsPoints;
            Treasure_GroupKills += rhs.Treasure_GroupKills;
            Treasure_GroupKillsPoints += rhs.Treasure_GroupKillsPoints;
            if (transferItems)
            {
                TakeItems(rhs, !takeItems);
            }
            Friends_SoloKills += rhs.Friends_SoloKills;
            Friends_SoloKillsPoints += rhs.Friends_SoloKillsPoints;
            Friends_GroupKills += rhs.Friends_GroupKills;
            Friends_GroupKillsPoints += rhs.Friends_GroupKillsPoints;
            Friends_BroughtFriendsPoints += rhs.Friends_BroughtFriendsPoints;
            Friends_BroughtFamilyBonus += rhs.Friends_BroughtFamilyBonus;
            Friends_FriendKilledCount += rhs.Friends_FriendKilledCount;
            Friends_FriendKilledPoints += rhs.Friends_FriendKilledPoints;
            Friends_KillEnemyFriendCount += rhs.Friends_KillEnemyFriendCount;
            Friends_KillEnemyFriendPoints += rhs.Friends_KillEnemyFriendPoints;
            Terr_SoloKills += rhs.Terr_SoloKills;
            Terr_SoloKillsPoints += rhs.Terr_SoloKillsPoints;
            Terr_GroupKills += rhs.Terr_GroupKills;
            Terr_GroupKillsPoints += rhs.Terr_GroupKillsPoints;
            Terr_TerritoriesCapturedCount += rhs.Terr_TerritoriesCapturedCount;
            Terr_TerritoriesCapturedPoints += rhs.Terr_TerritoriesCapturedPoints;
            Terr_TerritoriesOwnershipPoints += rhs.Terr_TerritoriesOwnershipPoints;
            Boss_SoloKills += rhs.Boss_SoloKills;
            Boss_SoloKillsPoints += rhs.Boss_SoloKillsPoints;
            Boss_GroupKills += rhs.Boss_GroupKills;
            Boss_GroupKillsPoints += rhs.Boss_GroupKillsPoints;
            Boss_BossHitsCount += rhs.Boss_BossHitsCount;
            Boss_BossHitsPoints += rhs.Boss_BossHitsPoints;
            Boss_BossKillsCount += rhs.Boss_BossKillsCount;
            Boss_BossKillsPoints += rhs.Boss_BossKillsPoints;
            return this;
        }

        /// <summary>
        /// Helper: returns total points for the current session type,
        /// summing only the relevant fields.
        /// You can expand this logic to handle any special bonus, etc.
        /// </summary>
        public int GetTotalPoints(eSessionTypes sessionType)
        {
            switch (sessionType)
            {
                case eSessionTypes.Deathmatch: // PvP Combats
                    return PvP_SoloKillsPoints + PvP_GroupKillsPoints;

                case eSessionTypes.CaptureTheFlag: // Flag Capture
                    return Flag_SoloKillsPoints +
                        Flag_GroupKillsPoints +
                        Flag_KillFlagCarrierPoints +
                        Flag_FlagReturnsPoints +
                        Flag_OwnershipPoints;

                case eSessionTypes.TreasureHunt: // Treasure Hunt
                    return Treasure_SoloKillsPoints +
                        Treasure_GroupKillsPoints +
                        Treasure_BroughtTreasuresPoints -
                        Treasure_StolenTreasuresPoints;

                case eSessionTypes.BringAFriend: // Bring Friends
                    return Friends_SoloKillsPoints +
                        Friends_GroupKillsPoints +
                        Friends_BroughtFriendsPoints +
                        Friends_BroughtFamilyBonus -
                        Friends_FriendKilledPoints +
                        Friends_KillEnemyFriendPoints;

                case eSessionTypes.TerritoryCapture: // Capture Territories
                    return Terr_SoloKillsPoints +
                        Terr_GroupKillsPoints +
                        Terr_TerritoriesCapturedPoints +
                        Terr_TerritoriesOwnershipPoints;

                case eSessionTypes.BossHunt: // Boss Kill Cooperation
                    return Boss_SoloKillsPoints +
                        Boss_GroupKillsPoints +
                        Boss_BossHitsPoints +
                        Boss_BossKillsPoints;

                default:
                    return 0;
            }
        }

        public string Serialize()
        {
            var playerScoreType = typeof(PvPScore);
            var properties = playerScoreType.GetProperties();
            return string.Join(
                ';', properties
                    .Where(p =>
                    {
                        if (p.GetCustomAttribute(typeof(IgnoreDataMemberAttribute)) != null)
                            return false;
                        
                        var filter = (DefaultValueAttribute?)p.GetCustomAttribute(typeof(DefaultValueAttribute));
                        var value = p.GetValue(this);
                        return filter == null || (filter.Value == null ? value != null : !filter.Value.Equals(value));
                    })
                    .Select(p => p.Name + "=" + p.GetValue(this))
            );
        }
    }

    public record PvPGroupScore(Guild Guild) : PvPScore(Guild)
    {
        public PvPGroupScore(Guild guild, IEnumerable<PvPScore> players) : this(guild)
        {
            Scores = new Dictionary<string, PvPScore>(players.Select(p => new KeyValuePair<string, PvPScore>(p.PlayerID, p)));
            foreach (var s in Scores.Values)
            {
                Add(s);
            }
        }
        
        public PvPGroupScore(Guild guild, IEnumerable<GamePlayer> players) : this(guild, players.Select(p => new PvPScore(p, false)))
        {
        }

        public Dictionary<string, PvPScore> Scores { get; init; } = new();

        public PvPScore Totals => this;
        public PvPScore GetOrCreateScore(GamePlayer player)
        {
            return GetOrCreateScore(player.InternalID!);
        }
        
        public PvPScore GetOrCreateScore(string playerId)
        {
            if (!Scores.TryGetValue(playerId, out var score))
            {
                score = new PvPScore(string.Empty, playerId, false);
                Scores[playerId] = score;
            }
            return score!;
        }

        /// <inheritdoc />
        public override PvPScore Add(PvPScore rhs, bool transferItems = true, bool takeItems = true)
        {
            base.Add(rhs, transferItems, takeItems);
            if (!Equals(rhs.PlayerID, PlayerID))
            {
                var nested = GetOrCreateScore(rhs.PlayerID);
                nested.Add(rhs, true, false); // we COPY the item data here; to be retrieved later when disbanding maybe
            }
            return this;
        }

        public int PlayerCount => Scores.Count;
    }
}
