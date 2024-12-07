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
using System.Linq;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS;
using DOL.Database;
using DOL.GS.Scripts;
using System.Collections.Generic;

namespace DOL.GS
{
    public class IllusionPet : GamePet
    {
        [Flags]
        public enum eIllusionFlags
        {
            None = 0,
            RandomizePositions = 1 << 0,
        }
        
        public IllusionPet(GamePlayer owner, eIllusionFlags mode) : base(new IllusionPetBrain(owner, mode))
        {
            PlayerCloner.ClonePlayer(owner, this, false, false, false);
        }

        /// <inheritdoc />
        public override long ExperienceValue
        {
            get => 0;
        }

        /// <inheritdoc />
        public override int RealmPointsValue
        {
            get => 0;
        }

        public int CloneMaxHealth
        {
            get;
            set;
        }

        public override int MaxHealth
        {
            get => CloneMaxHealth;
        }

        public override void Die(GameObject killer)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(this, this, 7202, 0, false, 1);
            }

            base.Die(killer);
        }

        /// <inheritdoc />
        public override int AttackSpeed(params InventoryItem[] weapons)
        {
            double speed = weapons == null || weapons.Length < 10 ? 30 : WeaponSpd;
            bool bowWeapon = true;

            if (weapons != null)
            {
                for (int i = 0; i < weapons.Length; i++)
                {
                    if (weapons[i] != null)
                    {
                        switch (weapons[i].Object_Type)
                        {
                            case (int)eObjectType.Fired:
                            case (int)eObjectType.Longbow:
                            case (int)eObjectType.Crossbow:
                            case (int)eObjectType.RecurvedBow:
                            case (int)eObjectType.CompositeBow:
                                break;
                            default:
                                bowWeapon = false;
                                break;
                        }
                    }
                }
            }

            int qui = Math.Min(250, (int)Quickness); //250 soft cap on quickness

            if (bowWeapon)
            {
                if (ServerProperties.Properties.ALLOW_OLD_ARCHERY)
                {
                    //Draw Time formulas, there are very many ...
                    //Formula 2: y = iBowDelay * ((100 - ((iQuickness - 50) / 5 + iMasteryofArcheryLevel * 3)) / 100)
                    //Formula 1: x = (1 - ((iQuickness - 60) / 500 + (iMasteryofArcheryLevel * 3) / 100)) * iBowDelay
                    //Table a: Formula used: drawspeed = bowspeed * (1-(quickness - 50)*0.002) * ((1-MoA*0.03) - (archeryspeedbonus/100))
                    //Table b: Formula used: drawspeed = bowspeed * (1-(quickness - 50)*0.002) * (1-MoA*0.03) - ((archeryspeedbonus/100 * basebowspeed))

                    //For now use the standard weapon formula, later add ranger haste etc.
                    speed *= (1.0 - (qui - 60) * 0.002);
                    double percent = 0;
                    // Calcul ArcherySpeed bonus to substract
                    percent = speed * 0.01 * GetModified(eProperty.ArcherySpeed);
                    // Apply RA difference
                    speed -= percent;
                    //log.Debug("speed = " + speed + " percent = " + percent + " eProperty.archeryspeed = " + GetModified(eProperty.ArcherySpeed));
                    if (RangedAttackType == eRangedAttackType.Critical)
                        speed = speed * 2 - (GetPlayerOwner()?.GetAbilityLevel(DOL.GS.Abilities.Critical_Shot) ?? 1 - 1) * speed / 10;
                }
                else
                {
                    // no archery bonus
                    speed *= (1.0 - (qui - 60) * 0.002);
                }
            }
            else
            {
                // TODO use haste
                //Weapon Speed*(1-(Quickness-60)/500]*(1-Haste)
                speed *= (1.0 - (qui - 60) * 0.002) * 0.01 * GetModified(eProperty.MeleeSpeed);
            }


            // apply speed cap
            if (GetPlayerOwner()?.IsInRvR == true)
            {
                if (speed < 15)
                {
                    speed = 15;
                }
            }
            else if (speed < 9)
            {
                speed = 9;
            }
            return (int)(speed * 100);
        }
    }
}