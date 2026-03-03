using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Tests;

public sealed class EventFeedFilterTests
{
    [Fact]
    public void Apply_FiltersByCategoryKeywordAndWindow()
    {
        var now = new DateTime(2026, 3, 3, 2, 0, 0, DateTimeKind.Utc);
        var entries = new[]
        {
            new EventFeedEntry { OccurredAtUtc = now.AddHours(-2), Category = "Trade", Title = "Buy", Detail = "ore contract" },
            new EventFeedEntry { OccurredAtUtc = now.AddMinutes(-20), Category = "Trade", Title = "Sell", Detail = "pirate-run shipment" },
            new EventFeedEntry { OccurredAtUtc = now.AddMinutes(-10), Category = "Intel", Title = "Alert", Detail = "pirate corridor" }
        };

        var options = new EventFeedFilterOptions
        {
            Category = "Trade",
            Keyword = "pirate",
            MaxAge = TimeSpan.FromHours(1)
        };

        var filtered = EventFeedFilter.Apply(entries, options, now);

        Assert.Single(filtered);
        Assert.Equal("Sell", filtered[0].Title);
    }
}
