namespace GalacticTrader.Desktop.Realtime;

public sealed class RealtimeReconnectPolicy
{
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);
    public double Multiplier { get; init; } = 2d;

    public TimeSpan GetDelay(int attempt)
    {
        var normalizedAttempt = Math.Max(1, attempt);
        var rawMilliseconds = InitialDelay.TotalMilliseconds * Math.Pow(Multiplier, normalizedAttempt - 1);
        var boundedMilliseconds = Math.Min(rawMilliseconds, MaxDelay.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(Math.Max(0, boundedMilliseconds));
    }
}
