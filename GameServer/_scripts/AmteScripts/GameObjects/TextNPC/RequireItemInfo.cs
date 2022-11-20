using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Scripts
{
    public class RequireItemInfo
    {
        public string ItemId
        {
            get;
            set;
        }

        public int Count
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }
    }


    public class EchangeurInfo
    {
        public IEnumerable<RequireItemInfo> requireInfos
        {
            get;
            set;
        }

        public GameInventoryItem GiveItem
        {
            get;
            set;
        }
    }
}