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

        protected override void CheckPlayerAggro()
        {
            if (Body.AttackState)
                return;
            foreach (GamePlayer pl in Body.GetPlayersInRadius((ushort)AggroRange))
            {
                if (!pl.IsAlive || pl.ObjectState != GameObject.eObjectState.Active || !GameServer.ServerRules.IsAllowedToAttack(Body, pl, true))
                    continue;


                if (pl.IsStealthed)
                    pl.Stealth(false);

                //Check Reputation
                if (pl.Reputation >= 0 && pl.Client.Account.PrivLevel == 1)
                {
                    //Full aggression against Non outlaws
                    AddToAggroList(pl, 1);
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