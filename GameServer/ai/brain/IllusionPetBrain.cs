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
using DOL.AI.Brain;
using DOL.GS;

namespace DOL.AI.Brain
{
    public class IllusionPetBrain : StandardMobBrain
    {
        private GameLiving m_owner;

        public IllusionPetBrain(GameLiving owner)
        {
            m_owner = owner;
            AggroLevel = 100;
            AggroRange = 500;
        }

        public override void Think()
        {
            if (Body == null || !Body.IsAlive)
                return;

            if (m_owner == null || !m_owner.IsAlive)
                return;

            // Follow the owner
            if (!Body.IsMoving && !Body.InCombat)
            {
                if (!Body.IsWithinRadius(m_owner, 150))
                {
                    Body.WalkTo(m_owner.Position.X, m_owner.Position.Y, m_owner.Position.Z, m_owner.MaxSpeedBase);
                }
            }

            // Attack the owner's target if the owner is in combat
            if (m_owner.InCombat && m_owner.TargetObject is GameLiving target && target.IsAlive)
            {
                if (!Body.IsAttacking || Body.TargetObject != target)
                {
                    Body.StartAttack(target);
                }
            }
        }
    }
}