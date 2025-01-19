namespace DOL.GameEvents
{
    public enum EventStatus
    {
        Idle,
        Starting,
        Started,
        Ending,
        EndedByTimer,
        EndedByKill,
        EndedByEventStarting,
        EndedByAreaEvent,
        EndedByTextNPC,
        EndedBySwitch
    }
}