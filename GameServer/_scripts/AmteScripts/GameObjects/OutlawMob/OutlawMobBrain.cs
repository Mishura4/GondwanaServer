using System;
using AmteScripts.Managers;
using DOL.gameobjects.CustomNPC;
using DOL.GS;
using DOL.GS.Scripts;

namespace DOL.AI.Brain
{
    public class OutlawMobBrain : GuardOutlawBrain
    {
        public override int CalculateAggroLevelToTarget(GameLiving target)
        {
			// only attack if green+ to target
			if (target.IsObjectGreyCon(Body))
				return 0;
            return base.CalculateAggroLevelToTarget(target);
        }
    }
}
