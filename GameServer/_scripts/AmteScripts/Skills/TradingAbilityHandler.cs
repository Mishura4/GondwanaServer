using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.SkillHandler
{
    /// <summary>
    /// Vol Ability Handler
    /// </summary>
    [SkillHandlerAttribute(Abilities.Trading)]
    public class TradingAbilityHandler : IAbilityActionHandler
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


        public void Execute(Ability Ab, GamePlayer player)
        {
            return;
        }

    }
}