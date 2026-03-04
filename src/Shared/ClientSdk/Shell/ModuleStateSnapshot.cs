namespace GalacticTrader.ClientSdk.Shell;

public sealed record ModuleStateSnapshot(
    GameplayModuleId ModuleId,
    ModuleUxState State,
    string Message,
    DateTimeOffset UpdatedAtUtc);
