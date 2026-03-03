using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class SpatialAudioMixFormatter
{
    public static string Build(SpeakerMixApiDto mix)
    {
        return
            $"{mix.PlayerId.ToString("N")[..8]} | " +
            $"distance {mix.Distance:N1} | " +
            $"gain {mix.Gain:N2} | " +
            $"pan {mix.Pan:N2}";
    }
}
