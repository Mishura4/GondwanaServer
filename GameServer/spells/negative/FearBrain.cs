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
    public class FearBrain : StandardMobBrain
    {
        /// <summary>
        /// Fixed thinking Interval for Fleeing
        /// </summary>
        public override int ThinkInterval
        {
            get
            {
                return 3000;
            }
        }

        private int m_timeWithoutPlayers;

        public bool IsPlayerIgnored(GamePlayer player)
        {
            if (!player.IsVisibleTo(Body))
                return true;

            if (!player.IsAlive)
                return true;

            return false;
        }

        /// <summary>
        /// Flee from Players on Brain Think
        /// </summary>
        public override void Think()
        {
            var range = Math.Max(AggroRange, 750);
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)(range * 1.25)))
            {
                if (IsPlayerIgnored(player))
                    continue;
                
                m_timeWithoutPlayers = 0;
                if (Body.IsReturningHome || GameMath.IsWithinRadius(player.Coordinate, Body.Coordinate, range))
                {
                    Body.CancelWalkToSpawn();
                    CalculateFleeTarget(player);
                    return;
                }
            }
            
            m_timeWithoutPlayers += ThinkInterval;
            if (m_timeWithoutPlayers >= 30000)
            {
                m_timeWithoutPlayers = 0;
                if (!Body.IsReturningHome && !Body.Coordinate.IsWithinDistance(Body.SpawnPosition, GameNPC.CONST_WALKTOTOLERANCE))
                {
                    Body.WalkToSpawn();
                }
            }
        }

        ///<summary>
        /// Calculate flee target.
        /// </summary>
        ///<param name="target">The target to flee.</param>
        protected virtual void CalculateFleeTarget(GameLiving target)
        {
            var targetAngle = Body.Coordinate.GetOrientationTo(target.Coordinate) + Angle.Degrees(180);

            Body.StopFollowing();
            Body.StopAttack();
            var destination = Body.Position + Vector.Create(targetAngle, length: 300);
            Body.PathTo(destination.Coordinate, Body.MaxSpeed);
        }

        ///<summary>
        /// Remove effect.
        /// </summary>
        ///<param name="target">The target to flee.</param>
        public virtual void RemoveEffect()
        {

        }

    }
}
