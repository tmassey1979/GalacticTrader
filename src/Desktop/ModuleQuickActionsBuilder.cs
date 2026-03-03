namespace GalacticTrader.Desktop;

public static class ModuleQuickActionsBuilder
{
    public static string Build(string? selectedModule)
    {
        var module = selectedModule?.Trim() ?? string.Empty;
        return module switch
        {
            "Dashboard" => "Quick Actions: Refresh Metrics | Refresh Events | Export Events",
            "Trading" => "Quick Actions: Preview Spread | Refresh Trades | Apply Listing Filters",
            "Routes" => "Quick Actions: Calculate Route | Optimize Profiles | Review Risk Panel",
            "Battles" => "Quick Actions: Refresh Outcomes | Monitor Ratings | Track Resource Delta",
            "Fleet" => "Quick Actions: Refresh Fleet | Inspect Upgrades | Review Crew Weighting",
            "Services" => "Quick Actions: Offer Contract | Negotiate Protection | Manage Blacklist",
            "Reputation" => "Quick Actions: Refresh Standings | Apply Alignment Action | Review Matrix",
            "Territory" => "Quick Actions: Assign Protection | Apply Policy | Recalculate Dominance",
            "Analytics" => "Quick Actions: Refresh Analytics | Export CSV | Review Leaderboards",
            "Comms" => "Quick Actions: Refresh Channels | Send Signal | Update Spatial Mix",
            _ => "Quick Actions: Use module controls to inspect strategic data"
        };
    }
}
