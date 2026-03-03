using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class CommunicationMessageProjectorTests
{
    [Fact]
    public void Build_OrdersMessagesByTimestamp_AndMapsFields()
    {
        var sender = Guid.NewGuid();
        var messages = new[]
        {
            new CommunicationChannelMessageApiDto
            {
                Id = Guid.NewGuid(),
                SenderId = sender,
                ChannelType = "Global",
                ChannelKey = "desk",
                Content = "second",
                IsModerated = true,
                CreatedAt = new DateTime(2026, 3, 3, 12, 30, 0, DateTimeKind.Utc)
            },
            new CommunicationChannelMessageApiDto
            {
                Id = Guid.NewGuid(),
                SenderId = Guid.Empty,
                ChannelType = "Global",
                ChannelKey = "desk",
                Content = "first",
                IsModerated = false,
                CreatedAt = new DateTime(2026, 3, 3, 12, 0, 0, DateTimeKind.Utc)
            }
        };

        var rows = CommunicationMessageProjector.Build(messages);

        Assert.Equal(2, rows.Count);
        Assert.Equal("first", rows[0].Content);
        Assert.Equal("system", rows[0].Sender);
        Assert.Equal("raw", rows[0].Moderation);
        Assert.Equal("filtered", rows[1].Moderation);
        Assert.Equal(sender.ToString("N")[..8], rows[1].Sender);
        Assert.Equal("Global:desk", rows[1].Channel);
    }
}
