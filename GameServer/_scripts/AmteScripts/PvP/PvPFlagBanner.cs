using AmteScripts.PvP.CTF;
using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class PvPFlagBanner : BannerVisual
    {
        /// <inheritdoc />
        public override void Drop()
        {
            var prev = CarryingPlayer;
            CarryingPlayer = null;
            if (Item is not FlagInventoryItem flagItem)
            {
                return;
            }

            flagItem.DropFlagOnGround(prev, null);
            base.Drop();
        }
    }
}
