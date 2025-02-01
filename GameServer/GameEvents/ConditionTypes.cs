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

    public enum FamilyConditionType
    {
        EndAction, // Event must be registered through EndingAction.StartEvent
        EndActionOrdered, // Event must be registered through EndingAction.StartEvent
        EventStarted, // Event must be started, can end
        EventStartedOrdered, // Event must be started, can have ended
        EventRunning, // Event must be currently running
        EventRunningOrdered, // Event must be currently running
    }
}