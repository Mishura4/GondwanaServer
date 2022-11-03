using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL
{
    public interface IGamePlayer
    {
        byte Level
        {
            get;
            set;
        }

        long GetCurrentMoney();

        bool HasAbility(string keyName);

        bool IsStealthed { get; }

        string GuildID { get; set; }

        Group Group { get; set; }

        int GetBaseSpecLevel(string keyname);

        bool IsAllowToVolInThisArea { get; set; }
    }
}