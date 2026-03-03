using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class VoiceQosSummaryFormatterTests
{
    [Fact]
    public void Build_ReturnsFallback_WhenSnapshotMissing()
    {
        var value = VoiceQosSummaryFormatter.Build(snapshot: null);
        Assert.Equal("No QoS sample.", value);
    }

    [Fact]
    public void Build_FormatsSnapshot()
    {
        var snapshot = new VoiceQosSnapshotApiDto
        {
            ParticipantCount = 5,
            SpeakingParticipants = 2,
            AverageLatencyMs = 37.28f,
            AverageJitterMs = 4.41f,
            AveragePacketLossPercent = 0.237f
        };

        var value = VoiceQosSummaryFormatter.Build(snapshot);

        Assert.Equal("Latency 37.3ms | Jitter 4.4ms | Loss 0.24% | Speakers 2/5", value);
    }
}
