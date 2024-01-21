using AmteScripts.Managers;
using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.AI.Brain
{
    public class GuardOutlawBrain
        : GuardNPCBrain
    {
        private bool _canBaf = true;

        public override bool CanBAF
        {
            get
            {
                return _canBaf;
            }
            set
            {
                _canBaf = true;
            }
        }

        public override int CalculateAggroLevelToTarget(GameLiving target)
        {
            if (target is GamePlayer player)
            {
                if (player.Reputation >= 0 && player.Client.Account.PrivLevel == 1)
                {
                    return 100;
                }
                return 0;
            }
            if (target.Realm == 0)
                return target.Level + 1;
            return base.CalculateAggroLevelToTarget(target);
        }

        protected override void CheckPlayerAggro()
        {
            if (Body.AttackState)
                return;
            foreach (GamePlayer pl in Body.GetPlayersInRadius((ushort)AggroRange))
            {
                if (!pl.IsAlive || pl.ObjectState != GameObject.eObjectState.Active || !GameServer.ServerRules.IsAllowedToAttack(Body, pl, true))
                    continue;

                int aggroLevel = CalculateAggroLevelToTarget(pl);
                if (aggroLevel > 0)
                {
                    if (pl.IsStealthed)
                        pl.Stealth(false);
                    //Full aggression against Non outlaws
                    AddToAggroList(pl, aggroLevel);
                    // Use new BAF system
                    BringFriends(pl);
                    continue;
                }
            }
        }

        protected override void CheckNPCAggro()
        {
            base.CheckNPCAggro();
        }
    }
}