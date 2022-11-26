using DOL.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.GameEvents
{
    public class GroupMobEvent
        : GameObjectEvent
    {
        public GroupMobEvent(string name)
            : base(name)
        {
        }

        public static readonly GroupMobEvent MobGroupDead = new GroupMobEvent("GroupMobEvent.MobGroupDead");
    }
}