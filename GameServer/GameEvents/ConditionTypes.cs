namespace DOL.GameEvents
{
    public enum StartingConditionType
    {
        Timer,
        Kill,
        Event,
        Money,
        Interval
    }

    public enum EndingConditionType
    {
        Timer,
        Kill,
        StartingEvent
    }
}