using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class VoiceQosSummaryFormatter
{
    public static string Build(VoiceQosSnapshotApiDto? snapshot)
    {
        if (snapshot is null)
        {
            return "No QoS sample.";
        }

        return
            $"Latency {snapshot.AverageLatencyMs:N1}ms | " +
            $"Jitter {snapshot.AverageJitterMs:N1}ms | " +
            $"Loss {snapshot.AveragePacketLossPercent:N2}% | " +
            $"Speakers {snapshot.SpeakingParticipants}/{snapshot.ParticipantCount}";
    }
}
