namespace GalacticTrader.ClientSdk.Shell;

public sealed record ModuleSwitchResult
{
    public bool Succeeded { get; init; }

    public bool NoOp { get; init; }

    public GameplayModuleId? ActiveModuleId { get; init; }

    public ModuleUxState State { get; init; }

    public string Message { get; init; } = string.Empty;

    public Exception? Exception { get; init; }

    public static ModuleSwitchResult Success(
        GameplayModuleId activeModuleId,
        ModuleUxState state,
        string message,
        bool noOp = false)
    {
        return new ModuleSwitchResult
        {
            Succeeded = true,
            NoOp = noOp,
            ActiveModuleId = activeModuleId,
            State = state,
            Message = message
        };
    }

    public static ModuleSwitchResult Failure(
        ModuleUxState state,
        string message,
        GameplayModuleId? activeModuleId = null,
        Exception? exception = null)
    {
        return new ModuleSwitchResult
        {
            Succeeded = false,
            NoOp = false,
            ActiveModuleId = activeModuleId,
            State = state,
            Message = message,
            Exception = exception
        };
    }
}
