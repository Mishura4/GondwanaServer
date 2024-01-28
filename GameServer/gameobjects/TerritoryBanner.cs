using DOL.Database;
using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class TerritoryBanner
        : GameStaticItem
    {

        public int OriginalEmblem { get; private set; }

        public TerritoryBanner()
            : base()
        {
        }

        public TerritoryBanner(int emblem)
            : base()
        {
            OriginalEmblem = emblem;
        }

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            this.OriginalEmblem = this.Emblem;
        }
    }
}