using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class ReputationBadgeProjector
{
    public static ReputationBadgeProjection Build(PlayerFactionStandingApiDto standing)
    {
        return standing.Tier.Trim().ToLowerInvariant() switch
        {
            "exalted" => new ReputationBadgeProjection { Badge = "Paragon", AccentHex = "#F4C542" },
            "honored" => new ReputationBadgeProjection { Badge = "Trusted", AccentHex = "#9BB8D6" },
            "friendly" => new ReputationBadgeProjection { Badge = "Friendly", AccentHex = "#4CC38A" },
            "neutral" => new ReputationBadgeProjection { Badge = "Neutral", AccentHex = "#8B9BB3" },
            "suspicious" => new ReputationBadgeProjection { Badge = "Watchlist", AccentHex = "#D68E54" },
            "hostile" => new ReputationBadgeProjection { Badge = "Hostile", AccentHex = "#E06767" },
            _ => new ReputationBadgeProjection { Badge = "Unknown", AccentHex = "#7EA1D9" }
        };
    }
}
