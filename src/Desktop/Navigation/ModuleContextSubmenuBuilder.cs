namespace GalacticTrader.Desktop.Navigation;

public static class ModuleContextSubmenuBuilder
{
    public static IReadOnlyList<string> Build(string? selectedModule)
    {
        var module = selectedModule?.Trim() ?? string.Empty;
        return module switch
        {
            "Dashboard" => ["Wealth Overview", "Fleet Overview", "Active Routes Summary"],
            "Trading" => ["Commodity Filters", "Margin Preview", "Market Heatmap"],
            "Routes" => ["Starmap Overlay", "Route Builder", "Risk Simulation"],
            "Battles" => ["Outcome Timeline", "Rating Delta", "Economic Impact"],
            "Fleet" => ["Ship Manifest", "Upgrade Modules", "Route Performance"],
            "Intel" => ["Signal Feed", "Threat Highlights", "Faction Reports"],
            "Market Intel" => ["Volatility Index", "Trade Flow", "Smuggling Corridors"],
            "Services" => ["Agent Roster", "Bias Distribution", "Contracts"],
            "Comms" => ["Channel Activity", "Spatial Mix", "Signal Log"],
            "Reputation" => ["Standing Matrix", "Impact Forecast", "Influence Zones"],
            "Territory" => ["Dominance Heatmap", "Protection Priority", "Policy Controls"],
            "Analytics" => ["Performance KPIs", "Risk Bands", "Leaderboards"],
            "Settings" => ["Preferences", "Hotkeys", "Persistence"],
            _ => ["Module Overview", "Primary Controls", "Detail Insights"]
        };
    }
}
