/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    /// <summary>
    /// GameMovingObject is a base class for boats and siege weapons.
    /// </summary>
    public class GameSiegeRam : GameSiegeWeapon
    {
        public GameSiegeRam()
            : base()
        {
            MeleeDamageType = eDamageType.Body;
            Name = "siege ram";

            //AmmoType = 0x3B00;
            //this.Effect = 0x8A1;
            AmmoType = 0x26;
            this.Model = 0xA2A;//0xA28
                               //TODO find all value for ram
            ActionDelay = new int[]
            {
                0,//none
				5000,//aiming
				10000,//arming
				0,//loading
				1100//fireing
			};//en ms
        }

        public override ushort Type()
        {
            return 0x9602;
        }

        public override int MAX_PASSENGERS
        {
            get
            {
                switch (Level)
                {
                    case 0:
                        return 2;
                    case 1:
                        return 6;
                    case 2:
                        return 8;
                    case 3:
                        return 12;
                }
                return Level * 3;
            }
        }

        public override int SLOT_OFFSET
        {
            get
            {
                return 1;
            }
        }

        public override void DoDamage()
        {
            GameLiving target = (TargetObject as GameLiving);
            if (target == null)
            {
                Owner.SendMessage("Select a target first.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            //todo good  distance check
            if (!this.IsWithinRadius(target, AttackRange))
            {
                Owner.SendMessage("You are too far away to attack " + target.Name, eChatType.CT_System,
                                      eChatLoc.CL_SystemWindow);
                return;
            }
            int damageAmount = RamDamage;

            //TODO: dps change by number
            target.TakeDamage(this, eDamageType.Crush, damageAmount, 0);
            Owner.SendMessage("The Ram hits " + target.Name + " for " + damageAmount + " dmg!", eChatType.CT_YouHit,
                                  eChatLoc.CL_SystemWindow);
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            {
                if (!(Owner == player))
                {
                    player.MessageFromArea(this, player.GetPersonalizedName(this) + " hits " + player.GetPersonalizedName(target), eChatType.CT_OthersCombat, eChatLoc.CL_SystemWindow);
                }
            }
            base.DoDamage();
        }

        public override bool RiderMount(GamePlayer rider, bool forced)
        {
            if (!base.RiderMount(rider, forced))
                return false;
            UpdateRamStatus();
            return true;
        }

        public override bool RiderDismount(bool forced, GamePlayer player)
        {
            if (!base.RiderDismount(forced, player))
                return false;
            if ((player as GamePlayer)?.SiegeWeapon == this)
                ReleaseControl();
            UpdateRamStatus();
            return true;
        }

        public override void ReleaseControl()
        {
            base.ReleaseControl();
            foreach (GamePlayer player in CurrentRiders)
                RiderDismount(true, player);
        }

        public void UpdateRamStatus()
        {
            //speed of reload changed by number
            ActionDelay[1] = GetReloadDelay;
        }

        private int GetReloadDelay
        {
            get
            {
                //custom formula
                return 10000 + ((Level + 1) * 2000) - 10000 * (int)((double)CurrentRiders.Length / (double)MAX_PASSENGERS);
            }
        }

        private int RamDamage
        {
            get
            {
                return BaseRamDamage + (int)(((double)BaseRamDamage / 2.0) * (double)((double)CurrentRiders.Length / (double)MAX_PASSENGERS));
            }
        }

        private int BaseRamDamage
        {
            get
            {
                int damageAmount = 0;
                switch (Level)
                {
                    case 0:
                        damageAmount = 200;
                        break;
                    case 1:
                        damageAmount = 300;
                        break;
                    case 2:
                        damageAmount = 450;
                        break;
                    case 3:
                        damageAmount = 750;
                        break;
                }
                return damageAmount;
            }
        }

        public override int AttackRange
        {
            get
            {
                switch (Level)
                {
                    case 0: return 300;
                    case 1: return 400;
                    case 2:
                    case 3: return 500;
                    default: return 500;
                }
            }
        }

        public override short MaxSpeed
        {
            get
            {
                //custom formula
                double speed = (10.0 + (5.0 * Level) + 100.0 * CurrentRiders.Length / MAX_PASSENGERS);
                foreach (GamePlayer player in CurrentRiders)
                {
                    RealmAbilities.RAPropertyEnhancer ab = player.GetAbility<RealmAbilities.LifterAbility>();
                    if (ab != null)
                        speed *= 1 + (ab.Amount / 100);
                }
                return (short)speed;
            }
        }
    }
}
