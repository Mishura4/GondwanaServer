using DOL.GS;

namespace DOL.AI.Brain
{
    /// <summary>
    /// Minimal dedicated brain for Occultist main pet.
    /// (Keeps ControlledNpcBrain behavior, so orders/aggro/casting stay familiar.)
    /// </summary>
    public class OccultistPetBrain : ControlledNpcBrain
    {
        public OccultistPetBrain(GameLiving owner) : base(owner)
        {
            // Feel free to tune defaults for the Occultist line
            AggroRange = 1500;
            WalkState = eWalkState.Follow;
            AggressionState = eAggressionState.Defensive;
        }

        // You can override ThinkInterval/CastInterval if you want it snappier:
        public override int ThinkInterval => 1200;
        public override int CastInterval
        {
            get { return 600; }
            set { /* ignore */ }
        }
    }
}