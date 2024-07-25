
using DOL.GS.Scripts;
using DOL.GS;
using System;

namespace DOL.Events
{
    public class SwitchEventArgs : EventArgs
    {
        public GameCoffre Coffre { get; }
        public GamePlayer Player { get; }

        public SwitchEventArgs(GameCoffre coffre, GamePlayer player)
        {
            Coffre = coffre;
            Player = player;
        }
    }
}