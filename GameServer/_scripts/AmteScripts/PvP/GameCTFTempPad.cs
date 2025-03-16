using System;
using System.Collections.Generic;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Geometry;
using DOL.Database;
using AmteScripts.Areas;
using AmteScripts.Managers;

namespace AmteScripts.PvP.CTF
{
    public class GameCTFTempPad : GameStaticItem
    {
        public GamePlayer OwnerPlayer { get; set; }

        private RegionTimer _ownershipTimer;
        private int _pointsInterval = 20_000;

        private RegionTimer _maxPadTimer;
        private const int MAX_TIME_ON_TEMP_PAD = 600_000;

        public GameFlagStatic OwnedFlag { get; private set; }

        public override bool AddToWorld()
        {
            if (!base.AddToWorld()) return false;
            var area = new TempPadArea(this, 90);
            CurrentRegion.AddArea(area);
            return true;
        }

        /// <summary>
        /// Called by FlagInventoryItem once the carrier enters the pad radius 
        /// to place the flag as a static item here.
        /// </summary>
        public void StartOwnershipTimer(GameFlagStatic flag, GamePlayer whoDroppedIt)
        {
            OwnedFlag = flag;
            flag.CurrentTempPad = this;
            // Start awarding points
            if (_ownershipTimer != null)
                _ownershipTimer.Stop();

            _ownershipTimer = new RegionTimer(this, TimerCallback, _pointsInterval);
            _ownershipTimer.Start(_pointsInterval);

            _maxPadTimer?.Stop();
            _maxPadTimer = new RegionTimer(this, MaxPadTimeCallback, MAX_TIME_ON_TEMP_PAD);
            _maxPadTimer.Start(MAX_TIME_ON_TEMP_PAD);
        }

        private int TimerCallback(RegionTimer timer)
        {
            // Award "Flag_OwnershipPoints" to the pad’s owner each interval
            if (OwnedFlag == null)
            {
                return 0;
            }

            if (OwnerPlayer != null)
            {
                var score = PvpManager.Instance.GetIndividualScore(OwnerPlayer);
                score.Flag_OwnershipPoints += 1;
            }

            return _pointsInterval;
        }

        private int MaxPadTimeCallback(RegionTimer t)
        {
            if (OwnedFlag != null)
            {
                var oldFlag = OwnedFlag;
                StopOwnership();
                oldFlag.RemoveFromWorld();

                if (oldFlag.BasePad != null)
                {
                    oldFlag.BasePad.RespawnFlag();
                }
            }
            return 0;
        }

        public void TryDropFlagIfCarried(GamePlayer player)
        {
            if (!IsPadOwner(player)) return;

            // Search backpack for a FlagInventoryItem
            for (eInventorySlot slot = eInventorySlot.FirstBackpack; slot <= eInventorySlot.LastBackpack; slot++)
            {
                var item = player.Inventory.GetItem(slot);
                if (item is FlagInventoryItem flagInv)
                {
                    var oldFlagRef = flagInv.FlagReference;
                    GameFlagBasePad originalPad = oldFlagRef?.BasePad;

                    flagInv.IsRemovalExpected = true;
                    flagInv.ClearFlagReference();
                    player.Inventory.RemoveItem(flagInv);

                    var staticFlag = new GameFlagStatic(originalPad);
                    staticFlag.SetOwnership(this.OwnerPlayer);
                    staticFlag.Name = flagInv.Name;
                    staticFlag.Model = (ushort)flagInv.Model;

                    staticFlag.Position = Position.Create(
                        this.CurrentRegionID,
                        this.Position.X,
                        this.Position.Y,
                        this.Position.Z,
                        this.Heading
                    );
                    staticFlag.AddToWorld();

                    OwnedFlag = staticFlag;
                    StartOwnershipTimer(staticFlag, player);

                    var score = PvpManager.Instance.GetIndividualScore(player);
                    score.Flag_FlagReturnsCount++;
                    score.Flag_FlagReturnsPoints += 20;

                    player.Out.SendMessage("You have deposited the flag on your outpost!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                    return;
                }
            }
        }

        private bool IsPadOwner(GamePlayer player)
        {
            if (this.OwnerPlayer != null)
                return (player == this.OwnerPlayer) || (player.Guild != null && player.Guild == this.OwnerPlayer.Guild);
            
            return false;
        }

        public override void Delete()
        {
            if (_ownershipTimer != null)
            {
                _ownershipTimer.Stop();
                _ownershipTimer = null;
            }
            if (OwnedFlag != null)
            {
                OwnedFlag.Reset();
                OwnedFlag = null;
            }
            base.Delete();
        }

        public void SetOwner(GamePlayer player)
        {
            this.OwnerPlayer = player;
        }

        public void StopOwnership()
        {
            if (_ownershipTimer != null) _ownershipTimer.Stop();
            if (_maxPadTimer != null) _maxPadTimer.Stop();
            _ownershipTimer = null;
            _maxPadTimer = null;
            OwnedFlag = null;
        }
    }

    public class TempPadArea : Area.Circle
    {
        private GameCTFTempPad _pad;

        public TempPadArea(GameCTFTempPad pad, int radius)
            : base($"{pad.Name}_Area", pad.Position.X, pad.Position.Y, pad.Position.Z, radius)
        {
            _pad = pad;
        }

        public override void OnPlayerEnter(GamePlayer player)
        {
            base.OnPlayerEnter(player);
            _pad.TryDropFlagIfCarried(player);
        }
    }
}