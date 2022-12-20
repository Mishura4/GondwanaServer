using System;
using System.Collections.Generic;
using System.Text;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.AI.Brain
{
    /// <summary>
    /// A brain that will destroy itself on player out of range.
    /// </summary>
    public class TempConsignmentBrain : StandardMobBrain
    {
        public GamePlayer Owner { get; set; }
        public TempConsignmentBrain(GamePlayer owner)
        {
            Owner = owner;
        }
        public override void Think()
        {
            if (Owner == null || Owner.CurrentRegion != Body.CurrentRegion || Owner.IsWithinRadius(Body, 1000) == false)
            {
                Body.Delete();
                Owner.Out.SendMessage(LanguageMgr.GetTranslation(Owner.Client, "Commands.Players.Market.Closed"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            base.Think();
        }
    }
}
