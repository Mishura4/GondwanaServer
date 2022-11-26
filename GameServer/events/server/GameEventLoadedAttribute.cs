using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.events.server
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class GameEventLoadedAttribute
        : Attribute
    {
    }
}