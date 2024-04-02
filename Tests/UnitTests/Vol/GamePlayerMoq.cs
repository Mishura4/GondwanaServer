using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.Vol
{
    public class GamePlayerMoq
        : IGamePlayer
    {
        public byte Level { get; set; }

        public bool IsAllowToVolInThisArea { get; set; }

        public bool IsStealthed { get; set; }

        public string GuildID { get; set; }

        public Group Group { get; set; }

        public int GetBaseSpecLevel(string keyname)
        {
            return 10;
        }

        public long GetCurrentMoney()
        {
            return 100L;
        }

        public bool HasAbility(string keyName)
        {
            return true;
        }
    }
}