using System;

namespace DOL.events.server
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class GameServerCoffreLoadedAttribute
        : Attribute
    {
    }
}