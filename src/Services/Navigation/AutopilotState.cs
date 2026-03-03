namespace GalacticTrader.Services.Navigation;

public enum AutopilotState
{
    Idle,
    Planning,
    Traveling,
    Paused,
    Encounter,
    Completed,
    Failed,
    Cancelled
}
