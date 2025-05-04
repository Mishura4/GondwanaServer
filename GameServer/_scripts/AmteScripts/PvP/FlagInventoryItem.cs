using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Database;
using DOL.Language;
using AmteScripts.PvP.CTF;
using AmteScripts.Managers;
using DOL.GS.Geometry;
using System.Linq;

namespace AmteScripts.PvP.CTF
{
    public class FlagInventoryItem : GameInventoryItem
    {
        public GameFlagStatic FlagReference { get; private set; }
        public bool IsRemovalExpected { get; set; } = false;
        private const int POINT_AWARD_INTERVAL_MS = 20000;
        private const int POINTS_PER_AWARD = 1;
        public FlagInventoryItem() : base() { }
        public FlagInventoryItem(ItemTemplate template) : base(template) { }

        public FlagInventoryItem(GameFlagStatic flagStatic) : base(CreateTemplateFromFlag(flagStatic))
        {
            FlagReference = flagStatic;
        }

        public override bool CanPersist => false;

        private static ItemTemplate CreateTemplateFromFlag(GameFlagStatic flagStatic)
        {
            ItemTemplate t = new ItemTemplate();
            t.Name = flagStatic.Name;
            t.Id_nb = "CTF_Flag_" + flagStatic.Name;
            t.Level = 50;
            t.Model = flagStatic.Model;
            t.IsDropable = false;
            t.IsPickable = true;
            t.IsTradable = false;
            t.Weight = 100;
            t.Quality = 100;
            t.DPS_AF = 0;
            t.Object_Type = (int)eObjectType.GenericItem;
            t.ClassType = typeof(FlagInventoryItem).FullName;
            return t;
        }

        /// <summary>
        /// When the carrier is killed or forcibly drops the flag.
        /// We remove from inventory, spawn the static item on the ground.
        /// </summary>
        public bool DropFlagOnGround(GamePlayer carrier, GameLiving? killer)
        {
            if (carrier == null)
                return false;

            this.IsRemovalExpected = true;
            if (!carrier.Inventory.RemoveItem(this))
                return false;

            GameFlagStatic newFlag = FlagReference ?? new GameFlagStatic(null);
            newFlag.Name = this.Name;
            newFlag.Model = (ushort)this.Model;
            newFlag.SetOwnership(null);

            var region = carrier.CurrentRegion;
            if (!newFlag.DropOnGround(
                    carrier.Position.X,
                    carrier.Position.Y,
                    carrier.Position.Z,
                    carrier.Heading,
                    region))
                return false;

            // Score: If killer is a player, award kill-carrier points
            if (killer is GamePlayer killerPlayer)
            {
                PvpManager.Instance.UpdateScores_FlagCarrierKill(killerPlayer, carrier, wasFlagCarrier: true);
            }

            foreach (GamePlayer plr in carrier.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).OfType<GamePlayer>())
            {
                plr.Out.SendSpellEffectAnimation(carrier, newFlag, 7043, 0, false, 0x01);
            }
            return true;
        }

        /// <summary>
        /// If the carrier hits the "TempAreaFlagPad," automatically drop onto that pad.
        /// </summary>
        public void DropFlagOnTempPad(GamePlayer carrier, GameCTFTempPad tempPad)
        {
            // remove from inventory
            this.IsRemovalExpected = true;
            carrier.Inventory.RemoveItem(this);

            // spawn a new static item at the pad
            GameFlagStatic newFlag = FlagReference ?? new GameFlagStatic(null);
            newFlag.Name = this.Name;
            newFlag.Model = (ushort)this.Model;
            newFlag.SetOwnership(carrier);

            newFlag.Position = Position.Create(tempPad.CurrentRegionID, tempPad.Position.X, tempPad.Position.Y, tempPad.Position.Z, tempPad.Heading);
            newFlag.AddToWorld();

            tempPad.StartOwnershipTimer(newFlag, carrier);
        }

        /// <summary>
        /// Called when this item is removed from a player's inventory.
        /// </summary>
        public override void OnLose(GamePlayer player)
        {
            base.OnLose(player);

            if (player.ActiveBanner?.Item == this)
            {
                player.ActiveBanner = null;
            }
            
            if (!IsRemovalExpected && PvpManager.Instance.IsOpen)
            {
                if (FlagReference != null && FlagReference.BasePad != null)
                {
                    FlagReference.BasePad.SpawnFlag();
                }
            }

            IsRemovalExpected = false;
        }

        /// <summary>
        /// Helper method to clear the flag reference.
        /// </summary>
        public void ClearFlagReference()
        {
            this.FlagReference = null;
        }

        public override bool CheckValid(GamePlayer player)
        {
            return true;
        }

        public override bool Use(GamePlayer player)
        {
            return false;
        }
    }
}