using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using GameServerScripts.Amtescripts.Managers;


namespace DOL.GS.Scripts
{
	public class OutlawMob : GuardOutlaw
    {
        public OutlawMob()
        {
            SetOwnBrain(new OutlawMobBrain());
        }
	}
}
