namespace GalacticTrader.ClientSdk.Trading;

public enum TradingOperationFailureState
{
    None = 0,
    InsufficientCredits = 1,
    InsufficientCargoCapacity = 2,
    InsufficientMarketQuantity = 3,
    RateLimited = 4,
    Unauthorized = 5,
    Validation = 6,
    Unknown = 7
}
