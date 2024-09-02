using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Scripts
{
    public class TeleportIndicator : GameNPC
    {
        public TeleportIndicator(TeleportNPC owner)
        {
            Owner = owner;
        }
        
        /// <inheritdoc />
        public override ushort GetModelForPlayer(GamePlayer player)
        {
            return (ushort)((Owner as TeleportNPC)?.ShouldShowInvisibleModel(player) == true ? 1923 : 667);
        }
    }
}
