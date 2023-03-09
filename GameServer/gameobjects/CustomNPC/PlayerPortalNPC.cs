using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS.Quests;
using DOL.GS.Scripts;
using log4net;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class PlayerPortalNPC : GameNPC
    {
        public PlayerPortal PortalSpell;
        public override bool Interact(GamePlayer player)
        {
            PortalSpell.TryTeleport(this, player);
            return true;
        }
    }

}
