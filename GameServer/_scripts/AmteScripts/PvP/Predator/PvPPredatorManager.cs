#nullable enable

using DOL.Events;
using DOL.GS;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AmteScripts.PvP
{
    public class PvPPredatorManager : AbstractPredatorManager
    {
    }

    public class WorldPredatorManager : AbstractPredatorManager
    {

        protected void OnPreyKilledHandler(DOLEvent e, object sender, EventArgs arguments)
        {
            if (arguments is not DyingEventArgs { Killer: GamePlayer playerKiller } args || sender is not GamePlayer playerVictim)
                return;

            CompleteBounty(playerKiller, playerVictim, args.PlayerKillers);
        }
    }
}
