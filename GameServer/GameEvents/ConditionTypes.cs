namespace DOL.GameEvents
{
    public enum StartingConditionType
    {
        Timer,
        Kill,
        Event,
        Money,
        Interval,
        Areaxevent,
        Quest,
        Switch
    }

    public enum EndingConditionType
    {
        Timer,
        Kill,
        StartingEvent,
        AreaEvent,
        TextNPC,
        Switch
    }
    
    public enum InstancedConditionTypes
    {
        All,
        Player,
        Group,
        Guild,
        Battlegroup,
        GroupOrSolo,
        GuildOrSolo,
    }
}