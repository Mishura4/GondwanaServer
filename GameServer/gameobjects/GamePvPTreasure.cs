using System;
using System.Reflection;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;

namespace DOL.GS
{
    /// <summary>
    /// A treasure item used for PvP Treasure Hunt sessions.
    /// When a player carries a PvPTreasure item and deposits it into a PvPChest,
    /// the treasure’s points (calculated as Condition/4) are added to the player's
    /// treasure-hunt score.
    /// </summary>
    public class PvPTreasure : GameInventoryItem
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        public PvPTreasure() : base() { }
        public PvPTreasure(ItemTemplate template) : base(template) { }
        public PvPTreasure(InventoryItem item) : base(item) { }

        /// <summary>
        /// Treasure points are computed as Condition divided by 4.
        /// </summary>
        public int TreasurePoints
        {
            get { return (int)(Condition / 4.0); }
        }

        public override bool Use(GamePlayer player)
        {
            return false;
        }
    }
}