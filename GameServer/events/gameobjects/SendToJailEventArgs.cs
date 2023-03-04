using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.events.gameobjects
{
    public class SendToJailEventArgs
        : EventArgs
    {
        public GamePlayer GamePlayer { get; set; }
        public int OriginalReputation { get; set; }
        public string SenderName { get; set; }
    }
}