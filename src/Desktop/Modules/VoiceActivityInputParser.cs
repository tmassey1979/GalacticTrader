namespace GalacticTrader.Desktop.Modules;

public static class VoiceActivityInputParser
{
    public static bool TryParseRms(string input, out float value)
    {
        if (!float.TryParse(input.Trim(), out var parsed))
        {
            value = 0f;
            return false;
        }

        value = Math.Clamp(parsed, 0f, 1f);
        return true;
    }

    public static bool TryParseMs(string input, out float value)
    {
        if (!float.TryParse(input.Trim(), out var parsed))
        {
            value = 0f;
            return false;
        }

        value = Math.Max(parsed, 0f);
        return true;
    }

    public static bool TryParsePercent(string input, out float value)
    {
        if (!float.TryParse(input.Trim(), out var parsed))
        {
            value = 0f;
            return false;
        }

        value = Math.Clamp(parsed, 0f, 100f);
        return true;
    }
}
