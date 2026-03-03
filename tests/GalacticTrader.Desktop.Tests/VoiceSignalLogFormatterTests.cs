using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class VoiceSignalLogFormatterTests
{
    [Fact]
    public void Build_FormatsBroadcastSignal()
    {
        var sender = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var signal = new VoiceSignalApiDto
        {
            SenderId = sender,
            TargetPlayerId = null,
            SignalType = "offer",
            Payload = "sdp-offer",
            CreatedAt = new DateTime(2026, 3, 3, 8, 15, 9, DateTimeKind.Utc)
        };

        var value = VoiceSignalLogFormatter.Build(signal);

        Assert.Equal("08:15:09Z | offer | from aaaaaaaa -> broadcast | sdp-offer", value);
    }

    [Fact]
    public void Build_FormatsTargetedSignal()
    {
        var sender = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var target = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var signal = new VoiceSignalApiDto
        {
            SenderId = sender,
            TargetPlayerId = target,
            SignalType = "answer",
            Payload = "sdp-answer",
            CreatedAt = new DateTime(2026, 3, 3, 8, 15, 10, DateTimeKind.Utc)
        };

        var value = VoiceSignalLogFormatter.Build(signal);

        Assert.Equal("08:15:10Z | answer | from bbbbbbbb -> cccccccc | sdp-answer", value);
    }
}
