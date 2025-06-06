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
using DOL.Events;
using DOL.GS;
using DOL.GS.Geometry;
using DOL.GS.ServerRules;
using System;
using System.Numerics;
using System.Runtime.InteropServices.ComTypes;
using Vector = DOL.GS.Geometry.Vector;

namespace DOL.AI.Brain
{
    public class IllusionPetBrain : StandardMobBrain
    {
        public GameLiving Owner => Body.Owner;

        private Vector m_followOffset = new Vector();
        private long m_lastFollowTick = 0;
        private Angle m_lastAngle = new();

        public IllusionPet.eIllusionFlags Mode
        {
            get;
            set;
        }
        
        public IllusionPetBrain(GameLiving owner, IllusionPet.eIllusionFlags mode)
        {
            AggroLevel = 100;
            AggroRange = 500;
            Mode = mode;
        }

        /// <inheritdoc />
        public override bool Start()
        {
            if (!base.Start())
                return false;
            
            GameEventMgr.AddHandler(Owner, GameLivingEvent.Moving, OnOwnerMove);
            GameEventMgr.AddHandler(Owner, GameLivingEvent.CastStarting, OnOwnerCast);
            GameEventMgr.AddHandler(Owner, GameLivingEvent.StyleExecute, OnOwnerStyle);
            GameEventMgr.AddHandler(Body, GameLivingEvent.CastFailed, OnActionEnd);
            GameEventMgr.AddHandler(Body, GameLivingEvent.CastFinished, OnActionEnd);
            GameEventMgr.AddHandler(Body, GameLivingEvent.CrowdControlExpired, OnActionEnd);
            return true;
        }
        
        private void OnOwnerStyle(DOLEvent e, object sender, EventArgs arguments)
        {
            if (sender is not GameLiving { IsAlive: true, ObjectState: GameObject.eObjectState.Active } || Body.IsIncapacitated)
                return;

            StyleEventArgs args = (StyleEventArgs)arguments;
            if (args.LastAttackData.IsCounterAttack)
                return;
            Body.NextCombatStyle = args.Style;
        }

        private void OnActionEnd(DOLEvent e, object sender, EventArgs arguments)
        {
            if (sender != Body)
                return;
            
            Follow();
        }

        private void OnOwnerCast(DOLEvent e, object sender, EventArgs arguments)
        {
            if (sender is not GameLiving { IsAlive: true, ObjectState: GameObject.eObjectState.Active } || Body.IsIncapacitated || Body.IsCasting)
                return;

            CastingEventArgs args = (CastingEventArgs)arguments;
            var spellTarget = (args.SpellHandler.Target == Owner ? Body : args.SpellHandler.Target);
            var previousTarget = Body.TargetObject;
            Body.StopMoving();
            Body.TargetObject = spellTarget;
            Body.CastSpell(args.SpellHandler.Spell, args.SpellHandler.SpellLine, true);
            Body.TargetObject = previousTarget;
        }

        private void OnOwnerMove(DOLEvent e, object sender, EventArgs arguments)
        {
            if (sender is not GameLiving { IsAlive: true, ObjectState: GameObject.eObjectState.Active } || Body.IsIncapacitated)
                return;

            long thisTick = GameServer.Instance.TickCount;
            if (thisTick - m_lastFollowTick < 500)
                return;
            
            m_lastFollowTick = GameServer.Instance.TickCount;
            Follow();
        }

        private void Follow()
        {
            var offset = Mode.HasFlag(IllusionPet.eIllusionFlags.RandomizePositions) ? Vector.Create(m_followOffset.X + Util.Random(-30, 30), m_followOffset.Y + Util.Random(-30, 30), 0) : m_followOffset;
            var targetPosition = Owner.Coordinate + offset;

            if (GameMath.IsWithinRadius2D(Body.Coordinate, targetPosition, GameNPC.CONST_WALKTOTOLERANCE))
                return;
            
            if (Body.AttackState)
            {
                if (!Owner.AttackState || Owner.TargetObject == null || !GameMath.IsWithinRadius2D(Owner.Coordinate, Owner.TargetObject.Coordinate, Owner.AttackRange + GameNPC.CONST_WALKTOTOLERANCE))
                {
                    Body.StopFollowing();
                    Body.TargetObject = null;
                    Body.AttackState = Owner.AttackState;
                }
                else
                {
                    return;
                }
            }
            if (Body.IsCasting)
                Body.StopCurrentSpellcast(); // NPCs don't automatically interrupt on moving, so do it manually
            Body.MaxSpeedBase = Owner.MaxSpeed;
            var speed = Owner.CurrentSpeed;
            speed = Math.Max(speed, (short)(Owner.MaxSpeedBase / 3));
            speed = Math.Min(speed, Owner.MaxSpeed);
            Body.PathTo(targetPosition, speed);
            m_lastAngle = Owner.Position.Orientation;
        }

        public override void Think()
        {
            if (Body is not { IsIncapacitated: false })
                return;

            if (Owner is not { IsAlive: true, ObjectState: GameObject.eObjectState.Active })
                return;

            if (!Body.AttackState && !Body.IsMoving)
            {
                if (m_lastAngle != Owner.Orientation)
                {
                    Body.TurnTo(Mode.HasFlag(IllusionPet.eIllusionFlags.RandomizePositions) ? Owner.Orientation + Angle.Radians(Util.RandomDouble() * Math.PI / 2 - Math.PI / 4) : Owner.Orientation);
                    m_lastAngle = Owner.Orientation;
                }
            }
            if (Body.AttackState && (!Body.InCombat || Body.TargetObject?.IsAttackable != true))
                Body.StopAttack();

            // Attack the owner's target if the owner is in combat
            if (Owner.InCombat && Owner.AttackState && Owner.TargetObject is GameLiving target && target.IsAlive)
            {
                if (!Body.AttackState || (Body.TargetObject != target && GameServer.ServerRules.IsAllowedToAttack(Body, target, true)))
                {
                    Body.StartAttack(target);
                }
            }
        }
        
        public void SetOffset(double xOffset, double yOffset)
        {
            m_followOffset = Vector.Create((int)(Math.Round(xOffset)), (int)(Math.Round(yOffset)), 0);
        }
    }
}