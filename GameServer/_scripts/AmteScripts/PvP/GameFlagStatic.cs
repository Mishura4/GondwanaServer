using System;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.GS.Geometry;
using log4net;
using AmteScripts.Managers;
using System.Linq;

namespace AmteScripts.PvP.CTF
{
    /// <summary>
    /// Static item version of the flag, placed on the base pad or dropped on ground or in a TempAreaFlagPad.
    /// </summary>
    public class GameFlagStatic : GamePvPStaticItem
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GameFlagStatic));
        public GameFlagBasePad BasePad { get; private set; }
        public GameCTFTempPad CurrentTempPad { get; set; }

        private RegionTimer _returnTimer;
        private const int RETURN_TIMEOUT_MS = 25000;
        private bool _isDroppedOnGround = false;

        public GameFlagStatic(GameFlagBasePad basePad)
        {
            BasePad = basePad;
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;

            if (OwnerPlayer == player)
            {
                player.Out.SendMessage("You already own this flag; you cannot pick it up again!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }
            if (OwnerGuild != null && player.Guild != null && OwnerGuild == player.Guild)
            {
                player.Out.SendMessage("Your group already owns this flag; you cannot pick it up again!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (OwnerPlayer != null && OwnerPlayer != player)
            {
                CaptureByEnemy(player);
                return true;
            }

            // Else, it's presumably on base pad or on ground => pick up
            return PlayerPickupFlag(player);
        }

        public bool PlayerPickupFlag(GamePlayer player)
        {
            if (!player.IsAlive)
            {
                player.Out.SendMessage("You can't pick up the flag while dead.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (player.IsStealthed)
            {
                player.Out.SendMessage("You can't pick up the flag while stealthed!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            var invFlag = new FlagInventoryItem(this);
            if (!player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invFlag))
            {
                player.Out.SendMessage("Your inventory is full!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            try
            {
                if (CurrentTempPad != null)
                {
                    CurrentTempPad.StopOwnership();
                    CurrentTempPad = null;
                }

                RemoveFromWorld();
                if (_returnTimer != null)
                {
                    _returnTimer.Stop();
                    _returnTimer = null;
                }

                if (BasePad != null)
                {
                    BasePad.NotifyFlagLeft();
                }

                SetOwnership(null);

                ushort effectID = (ushort)Util.Random(5811, 5815);
                foreach (GamePlayer plr in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).OfType<GamePlayer>())
                {
                    plr.Out.SendSpellEffectAnimation(player, player, effectID, 0, false, 0x01);
                }

                var banner = new PvPFlagBanner()
                {
                    OwningPlayer = player,
                    Item = invFlag,
                };
                if (player.IsInPvP)
                    banner.Emblem = PvpManager.Instance.GetEmblemForPlayer(player);

                player.ActiveBanner = banner;
                _isDroppedOnGround = false;

                foreach (var area in player.CurrentAreas)
                {
                    if (area is TempPadArea padArea)
                    {
                        // Flag picked up in our pad, possible if we just killed a thief
                        padArea.Pad.TryDropFlagIfCarried(player);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                // Something went wrong, abort & reset
                player.Inventory.RemoveItem(invFlag);
                log.Error(e);
                BasePad.SpawnFlag();
                return false;
            }
        }

        public void CaptureByEnemy(GamePlayer captor)
        {
            if (CurrentTempPad != null)
            {
                CurrentTempPad.StopOwnership();
                CurrentTempPad = null;
            }
            
            SetOwnership(null);

            bool success = PlayerPickupFlag(captor);
            if (success)
            {
                ushort effectID = (ushort)Util.Random(5811, 5815);
                foreach (var plr in captor.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).OfType<GamePlayer>())
                {
                    plr.Out.SendSpellEffectAnimation(captor, captor, effectID, 0, false, 0x01);
                }
            }
        }

        /// <summary>
        /// Called if the carrier was killed, or forcibly dropped the flag on the ground,
        /// to spawn this static item at the location of the kill.
        /// If no one picks it up for X seconds, it returns to base.
        /// </summary>
        public bool DropOnGround(int x, int y, int z, ushort heading, Region region)
        {
            this.Position = Position.Create(region.ID, x, y, z, heading);
            if (!this.AddToWorld())
                return false;
            
            _isDroppedOnGround = true;

            if (_returnTimer != null)
            {
                _returnTimer.Stop();
            }

            _returnTimer = new RegionTimer(this, (t) => OnReturnTimerCallback(t));
            _returnTimer.Start(RETURN_TIMEOUT_MS);
            return true;
        }

        public bool Reset()
        {
            if (BasePad != null)
            {
                RemoveFromWorld();
                BasePad.RespawnFlag();
            }
            else
            {
                RemoveFromWorld();
            }
            return true;
        }

        private int OnReturnTimerCallback(RegionTimer timer)
        {
            Reset();
            return 0;
        }
    }
}
