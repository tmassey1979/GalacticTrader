using System;

namespace GalacticTrader.Data.Models
{
    /// <summary>
    /// Represents a trade transaction
    /// </summary>
    public class TradeTransaction
    {
        public Guid Id { get; set; }
        public Guid PlayerId { get; set; }
        public Guid SellerId { get; set; } // NPC or another player
        public Guid CommodityId { get; set; }
        public Guid FromMarketId { get; set; }
        public Guid ToMarketId { get; set; }
        
        // Transaction Details
        public long Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal TotalPrice { get; set; }
        
        // Fees & Costs
        public decimal Tariff { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TransactionFee { get; set; }
        public decimal InsuranceCost { get; set; }
        
        // Final Amount
        public decimal NetProfit { get; set; }
        
        // Status
        public string Status { get; set; } // "pending", "completed", "failed"
        public bool UsedSmugglingRoute { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        
        // Navigation
        public Player Buyer { get; set; }
        public Commodity Commodity { get; set; }
        public Market FromMarket { get; set; }
        public Market ToMarket { get; set; }
    }
}
