namespace GalacticTrader.Desktop.Modules;

public static class ServicesContractLogFormatter
{
    public static string Build(DateTime atUtc, string action, string agentName, string detail)
    {
        var normalizedAction = string.IsNullOrWhiteSpace(action) ? "action" : action.Trim();
        var normalizedAgent = string.IsNullOrWhiteSpace(agentName) ? "unknown-agent" : agentName.Trim();
        var normalizedDetail = string.IsNullOrWhiteSpace(detail) ? "completed" : detail.Trim();
        return $"{atUtc:HH:mm:ss}Z | {normalizedAction} | {normalizedAgent} | {normalizedDetail}";
    }
}
