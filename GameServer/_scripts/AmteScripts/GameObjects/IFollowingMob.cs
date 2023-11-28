using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOL.GS;

namespace DOL.GS.Scripts
{
    public interface IFollowingMob
    {
        public void Follow(GameObject obj);

        public void StopFollowing();

        public void Reset();
    }
}
