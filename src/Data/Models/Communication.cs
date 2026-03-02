using System;

namespace GalacticTrader.Data.Models
{
    /// <summary>
    /// Persisted channel message for text communications.
    /// </summary>
    public class ChannelMessage
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public string ChannelType { get; set; } = string.Empty; // global, sector, faction, private, fleet
        public string ChannelKey { get; set; } = string.Empty; // e.g. sector id, faction id, private thread id
        public string Content { get; set; } = string.Empty;
        public bool IsModerated { get; set; }
        public DateTime CreatedAt { get; set; }

        public Player? Sender { get; set; }
    }
}
