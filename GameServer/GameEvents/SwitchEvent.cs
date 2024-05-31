using DOL.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.GameEvents
{
    public class SwitchEvent : GameObjectEvent
    {
        public SwitchEvent(string name) : base(name) { }

        public static readonly SwitchEvent SwitchActivated = new SwitchEvent("SwitchEvent.SwitchActivated");
    }
}