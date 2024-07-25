using DOL.Events;
using DOL.GS.GameEvents;
using System;

namespace DOL.GS
{
    public class SwitchEventHandler : IDOLEventHandler
    {
        public void Notify(DOLEvent e, object sender, EventArgs args)
        {
            if (e == SwitchEvent.SwitchActivated)
            {
                SwitchEventArgs switchArgs = (SwitchEventArgs)args;
                // Handle the switch activation
                // Example: Logging or triggering further actions
                Console.WriteLine($"Switch activated by player {switchArgs.Player.Name} on coffre {switchArgs.Coffre.Name}");
            }
        }
    }
}