using DOL.AI;
using DOL.AI.Brain;
using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class StaticMob
        : GameNPC
    {

        public StaticMob(ABrain aBrain)
            : base(aBrain)
        {
            this.SetOwnBrain(aBrain);
        }

        public StaticMob()
            : base()
        {
        }

        public override ABrain SetOwnBrain(ABrain brain)
        {
            return base.SetOwnBrain(new StaticMobBrain());
        }

        public override bool IsBeingInterrupted => false;
    }
}

namespace DOL.AI.Brain
{
    public class StaticMobBrain
    : StandardMobBrain
    {

        public StaticMobBrain()
            : base()
        {
        }

        public override bool CanRandomWalk => false;

        protected override void OnAttackedByEnemy(AttackData ad)
        {
            if (!Body.AttackState
           && Body.IsAlive
           && Body.ObjectState == GameObject.eObjectState.Active)
            {
                if (!this.AggroTable.ContainsKey(ad.Attacker))
                    AddToAggroList(ad.Attacker, 1);
            }

            this.AttackMostWanted();
        }

        protected override void AttackMostWanted()
        {
            if (!IsActive)
                return;

            Body.TargetObject = CalculateNextAttackTarget();

            this.CheckSpells(eCheckSpellType.Offensive);
        }
    }
}