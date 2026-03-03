namespace GalacticTrader.Desktop.Routes;

public static class RouteModePresetMapper
{
    public static string ToApiMode(string? presetLabel)
    {
        if (string.IsNullOrWhiteSpace(presetLabel))
        {
            return "Standard";
        }

        return presetLabel.Trim() switch
        {
            "Safe Route" => "Convoy",
            "Balanced Route" => "Standard",
            "High Profit Route" => "HighBurn",
            "Smuggler Route" => "GhostRoute",
            // Backward compatibility with previous dropdown values.
            "Standard" => "Standard",
            "HighBurn" => "HighBurn",
            "StealthTransit" => "StealthTransit",
            "Convoy" => "Convoy",
            "GhostRoute" => "GhostRoute",
            "ArmedEscort" => "ArmedEscort",
            _ => "Standard"
        };
    }
}
