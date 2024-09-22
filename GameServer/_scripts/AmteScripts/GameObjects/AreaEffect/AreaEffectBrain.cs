using DOL.GS;
using DOL.GS.Scripts;

namespace DOL.AI.Brain
{
    public class AreaEffectBrain : APlayerVicinityBrain
    {
        public override int ThinkInterval
        {
            get { return 1000; }
        }

        public override void Think()
        {
            AreaEffect areaEffect = Body as AreaEffect;

            if (areaEffect != null)
            {
                areaEffect.CheckGroupMob();
                if (areaEffect.SpellID != 0)
                    areaEffect.ApplySpell();
                else
                    areaEffect.ApplyEffect();
                AreaEffect nextArea = areaEffect.CheckFamily();
                if (nextArea != null && areaEffect.IntervalMin > 0)
                    new NextAreaTimer(nextArea).Start(areaEffect.IntervalMin * 1000);
                else if (nextArea != null)
                    new NextAreaTimer(nextArea).Start(1);
            }
        }

        private class NextAreaTimer : GameTimer
        {
            private AreaEffect nextArea;

            public NextAreaTimer(AreaEffect actionSource) : base(actionSource.CurrentRegion.TimeManager)
            {
                nextArea = actionSource;
            }

            public override void OnTick()
            {
                nextArea.CallAreaEffect();
                Stop();
            }
        }
    }
}
