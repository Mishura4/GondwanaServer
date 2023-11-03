using System;
using AmteScripts.Managers;
using DOL.gameobjects.CustomNPC;
using DOL.GS;
using DOL.GS.Scripts;
using System.Numerics;

namespace DOL.AI.Brain
{
    public class MageMobBrain : AmteMobBrain
    {
        /// <summary>
        /// Flee from close Players or on low mana on Brain Think
        /// </summary>
        public override void Think()
        {

            foreach (GamePlayer player in Body.GetPlayersInRadius(350))
            {
                CalculateFleeTarget(player);
                return;
            }

            if (Body.Mana < Body.MaxMana * 0.15)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(750))
                {
                    CalculateFleeTarget(player);
                    return;
                }
            }
            base.Think();
        }

        ///<summary>
        /// Calculate flee target.
        /// </summary>
        ///<param name="target">The target to flee.</param>
        protected virtual void CalculateFleeTarget(GameLiving target)
        {
            ushort TargetAngle = (ushort)((Body.GetHeading(target) + 2048) % 4096);

            var fleePoint = Body.GetPointFromHeading(TargetAngle, 300);
            var point = PathingMgr.Instance.GetClosestPoint(Body.CurrentZone, new Vector3(fleePoint, Body.Position.Z), 128, 128, 256);
            Body.StopFollowing();
            Body.StopAttack();
            Body.PathTo(point.HasValue ? point.Value : new Vector3(fleePoint, Body.Position.Z), Body.MaxSpeed);
        }

    }
}
