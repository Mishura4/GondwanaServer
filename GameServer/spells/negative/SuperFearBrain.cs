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
using DOL.GS;
using DOL.GS.Geometry;

namespace DOL.AI.Brain
{
    public class SuperFearBrain : FearBrain
    {
        short m_maxSpeedBuff = 0;
        
        ///<summary>
        /// Calculate flee target.
        /// </summary>
        ///<param name="target">The target to flee.</param>
        protected override void CalculateFleeTarget(GameLiving target)
        {
            var targetAngle = Body.Coordinate.GetOrientationTo(target.Coordinate) + Angle.Degrees(180);
            var speed = (short)(Body.MaxSpeed * 1.5); // 150% speed
            var destination = Body.Coordinate + Vector.Create(targetAngle, speed * 3.5); // Flee for 3.5 seconds
            if (Body.MaxDistance > 0 && !destination.IsWithinDistance(Body.Home, Body.MaxDistance))
            {
                var angleToSpawn = Body.Home.Coordinate.GetOrientationTo(destination);
                destination = Body.Home.Coordinate + Vector.Create(angleToSpawn, Body.MaxDistance);
            }
            
            var point = PathingMgr.Instance.GetClosestPointAsync(Body.CurrentZone, destination, 128, 128, 256);
            Body.StopFollowing();
            Body.StopAttack();
            Body.PathTo(point.HasValue ? Coordinate.Create(point.Value) : destination, Body.MaxSpeed);
            if (Body.Motion.Destination.IsWithinDistance(Body.Motion.CurrentPosition, 0.5, true))
            {
                return;
            }
            
            Body.CurrentSpeed = (short)(Body.MaxSpeed * 1.5);
        }

        /// <inheritdoc />
        protected override void SetBody(GameNPC npc)
        {
            base.SetBody(npc);
        
            npc.AbilityBonus[eProperty.MaxSpeed] += 50;
        }

        //on removal of the brain set speed to normal
        public override void RemoveEffect()
        {
            Body.MaxSpeedBase = (short)(Body.MaxSpeedBase - m_maxSpeedBuff);
        }
    }
}
