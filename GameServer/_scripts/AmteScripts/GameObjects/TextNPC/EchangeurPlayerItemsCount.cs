using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Scripts
{
    public class EchangeurPlayerItemsCount
    {
        public Dictionary<string, int> Items
        {
            get;
            set;
        }

        public bool HasAllRequiredItems
        {
            get;
            set;
        }
    }
}