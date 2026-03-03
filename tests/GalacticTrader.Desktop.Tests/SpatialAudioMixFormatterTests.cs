using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class SpatialAudioMixFormatterTests
{
    [Fact]
    public void Build_FormatsSpeakerMixLine()
    {
        var mix = new SpeakerMixApiDto
        {
            PlayerId = Guid.Parse("abcdefab-cdef-cdef-cdef-abcdefabcdef"),
            Distance = 37.41f,
            Gain = 0.527f,
            Pan = -0.18f
        };

        var value = SpatialAudioMixFormatter.Build(mix);

        Assert.Equal("abcdefab | distance 37.4 | gain 0.53 | pan -0.18", value);
    }
}
