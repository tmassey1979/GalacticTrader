using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class VoiceSignalLogFormatter
{
    public static string Build(VoiceSignalApiDto signal)
    {
        var target = signal.TargetPlayerId.HasValue
            ? signal.TargetPlayerId.Value.ToString("N")[..8]
            : "broadcast";

        return
            $"{DateTime.SpecifyKind(signal.CreatedAt, DateTimeKind.Utc):HH:mm:ss}Z | " +
            $"{signal.SignalType} | " +
            $"from {signal.SenderId.ToString("N")[..8]} -> {target} | " +
            $"{signal.Payload}";
    }
}
