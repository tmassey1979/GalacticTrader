namespace GalacticTrader.ClientSdk.Fleet;

public enum FleetOperationFailureState
{
    None = 0,
    Validation = 1,
    Unauthorized = 2,
    RateLimited = 3,
    Unknown = 4
}
