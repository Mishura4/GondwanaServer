using System;
using DOL.GS;
using DOL.GS.Geometry;

namespace DOL.AI.Brain
{
    /// <summary>
    /// Fear brain for pets/servants: flee specifically from the caster who applied the effect,
    /// and silence owner commands while active (via TempProperties flag set by the spell).
    /// </summary>
    public class PetFearBrain : FearBrain
    {
        private readonly WeakReference<GameLiving> _fearedFrom;

        public PetFearBrain(GameLiving caster)
        {
            _fearedFrom = new WeakReference<GameLiving>(caster);
        }

        public override int ThinkInterval => 3000;

        public override void Think()
        {
            if (!_fearedFrom.TryGetTarget(out var feared) || feared == null || !feared.IsAlive)
            {
                base.Think();
                return;
            }

            CalculateFleeTarget(feared);
        }

        public override void RemoveEffect()
        {
            Body?.TempProperties?.removeProperty("FEAR_SERVANT_ACTIVE");
            Body?.StopFollowing();
            Body?.StopAttack();
            Body!.TargetObject = null;

            if (Body?.Owner != null && Body.Brain is ControlledNpcBrain cb && cb.WalkState == eWalkState.Follow)
                cb.FollowOwner();
        }

        /// <summary>
        /// Optionally bump flee distance a bit for pets so they de-sync from owner quickly.
        /// </summary>
        protected override void CalculateFleeTarget(GameLiving target)
        {
            var targetAngle = Body.Coordinate.GetOrientationTo(target.Coordinate) + Angle.Degrees(180);
            Body.StopFollowing();
            Body.StopAttack();

            var destination = Body.Position + Vector.Create(targetAngle, length: 350);
            Body.PathTo(destination.Coordinate, Body.MaxSpeed);
        }
    }
}