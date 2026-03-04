using Microsoft.EntityFrameworkCore;
using GalacticTrader.Data.Models;

namespace GalacticTrader.Data
{
    /// <summary>
    /// Main database context for GalacticTrader
    /// </summary>
    public class GalacticTraderDbContext : DbContext
    {
        public GalacticTraderDbContext(DbContextOptions<GalacticTraderDbContext> options) : base(options)
        {
        }

        // Players & Characters
        public DbSet<Player> Players { get; set; }
        public DbSet<Crew> Crew { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }

        // Ships & Fleet
        public DbSet<Ship> Ships { get; set; }
        public DbSet<Cargo> Cargo { get; set; }
        public DbSet<ShipModule> ShipModules { get; set; }

        // Navigation
        public DbSet<Sector> Sectors { get; set; }
        public DbSet<Route> Routes { get; set; }

        // Economy & Trading
        public DbSet<Commodity> Commodities { get; set; }
        public DbSet<Market> Markets { get; set; }
        public DbSet<MarketListing> MarketListings { get; set; }
        public DbSet<MarketPriceHistory> MarketPriceHistories { get; set; }
        public DbSet<TradeTransaction> TradeTransactions { get; set; }

        // Factions
        public DbSet<Faction> Factions { get; set; }
        public DbSet<PlayerFactionRelationship> PlayerFactionRelationships { get; set; }

        // Combat
        public DbSet<CombatLog> CombatLogs { get; set; }

        // NPCs
        public DbSet<NPCAgent> NPCAgents { get; set; }
        public DbSet<NPCShip> NPCShips { get; set; }

        // Communication
        public DbSet<ChannelMessage> ChannelMessages { get; set; }

        // Leaderboards
        public DbSet<Leaderboard> Leaderboards { get; set; }

        // Strategic Systems (Phase 1)
        public DbSet<SectorVolatilityCycle> SectorVolatilityCycles { get; set; }
        public DbSet<CorporateWar> CorporateWars { get; set; }
        public DbSet<InfrastructureOwnership> InfrastructureOwnerships { get; set; }
        public DbSet<TerritoryDominance> TerritoryDominances { get; set; }

        // Strategic Systems (Phase 2)
        public DbSet<InsurancePolicy> InsurancePolicies { get; set; }
        public DbSet<InsuranceClaim> InsuranceClaims { get; set; }
        public DbSet<IntelligenceNetwork> IntelligenceNetworks { get; set; }
        public DbSet<IntelligenceReport> IntelligenceReports { get; set; }

        // Realtime Terra Colonist Logistics
        public DbSet<TerraColonistSource> TerraColonistSources { get; set; }
        public DbSet<ColonistShipment> ColonistShipments { get; set; }
        public DbSet<ColonistDeliveryAudit> ColonistDeliveryAudits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Player
            modelBuilder.Entity<Player>()
                .HasKey(p => p.Id);
            modelBuilder.Entity<Player>()
                .Property(p => p.NetWorth)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.KeycloakUserId)
                .IsUnique();
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.Username)
                .IsUnique();

            // Configure UserAccount
            modelBuilder.Entity<UserAccount>()
                .HasKey(u => u.Id);
            modelBuilder.Entity<UserAccount>()
                .HasIndex(u => u.Username)
                .IsUnique();
            modelBuilder.Entity<UserAccount>()
                .HasIndex(u => u.Email)
                .IsUnique();
            modelBuilder.Entity<UserAccount>()
                .HasIndex(u => u.KeycloakId)
                .IsUnique();
            modelBuilder.Entity<UserAccount>()
                .Property(u => u.Roles)
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList());

            // Configure Ship
            modelBuilder.Entity<Ship>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<Ship>()
                .HasOne(s => s.Player)
                .WithMany(p => p.Ships)
                .HasForeignKey(s => s.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Ship>()
                .HasOne(s => s.CurrentSector)
                .WithMany(sec => sec.DockedShips)
                .HasForeignKey(s => s.CurrentSectorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Sector
            modelBuilder.Entity<Sector>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<Sector>()
                .HasIndex(s => s.Name);

            // Configure Route (edges between sectors)
            modelBuilder.Entity<Route>()
                .HasKey(r => r.Id);
            modelBuilder.Entity<Route>()
                .HasOne(r => r.FromSector)
                .WithMany(s => s.OutboundRoutes)
                .HasForeignKey(r => r.FromSectorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Route>()
                .HasOne(r => r.ToSector)
                .WithMany(s => s.InboundRoutes)
                .HasForeignKey(r => r.ToSectorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Crew
            modelBuilder.Entity<Crew>()
                .HasKey(c => c.Id);
            modelBuilder.Entity<Crew>()
                .HasOne(c => c.Player)
                .WithMany(p => p.Crew)
                .HasForeignKey(c => c.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Crew>()
                .HasOne(c => c.Ship)
                .WithMany(s => s.Crew)
                .HasForeignKey(c => c.ShipId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Faction
            modelBuilder.Entity<Faction>()
                .HasKey(f => f.Id);
            modelBuilder.Entity<Faction>()
                .HasIndex(f => f.Name)
                .IsUnique();

            // Configure PlayerFactionRelationship
            modelBuilder.Entity<PlayerFactionRelationship>()
                .HasKey(pfr => pfr.Id);
            modelBuilder.Entity<PlayerFactionRelationship>()
                .HasOne(pfr => pfr.Player)
                .WithMany(p => p.FactionRelationships)
                .HasForeignKey(pfr => pfr.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PlayerFactionRelationship>()
                .HasOne(pfr => pfr.Faction)
                .WithMany(f => f.PlayerRelationships)
                .HasForeignKey(pfr => pfr.FactionId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PlayerFactionRelationship>()
                .HasIndex(pfr => new { pfr.PlayerId, pfr.FactionId })
                .IsUnique();

            // Configure Market
            modelBuilder.Entity<Market>()
                .HasKey(m => m.Id);
            modelBuilder.Entity<Market>()
                .HasOne(m => m.Sector)
                .WithMany(s => s.Markets)
                .HasForeignKey(m => m.SectorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure MarketListing
            modelBuilder.Entity<MarketListing>()
                .HasKey(ml => ml.Id);
            modelBuilder.Entity<MarketListing>()
                .HasOne(ml => ml.Market)
                .WithMany(m => m.Listings)
                .HasForeignKey(ml => ml.MarketId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Commodity
            modelBuilder.Entity<Commodity>()
                .HasKey(c => c.Id);
            modelBuilder.Entity<Commodity>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // Configure TradeTransaction
            modelBuilder.Entity<TradeTransaction>()
                .HasKey(tt => tt.Id);
            modelBuilder.Entity<TradeTransaction>()
                .HasOne(tt => tt.Buyer)
                .WithMany(p => p.TradeTransactions)
                .HasForeignKey(tt => tt.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TradeTransaction>()
                .Property(tt => tt.TotalPrice)
                .HasPrecision(18, 2);

            // Configure CombatLog
            modelBuilder.Entity<CombatLog>()
                .HasKey(cl => cl.Id);
            modelBuilder.Entity<CombatLog>()
                .HasOne(cl => cl.Attacker)
                .WithMany(p => p.CombatLogs)
                .HasForeignKey(cl => cl.AttackerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Leaderboard
            modelBuilder.Entity<Leaderboard>()
                .HasKey(l => l.Id);
            modelBuilder.Entity<Leaderboard>()
                .HasIndex(l => new { l.LeaderboardType, l.Rank });

            // Configure SectorVolatilityCycle
            modelBuilder.Entity<SectorVolatilityCycle>()
                .HasKey(cycle => cycle.Id);
            modelBuilder.Entity<SectorVolatilityCycle>()
                .HasOne(cycle => cycle.Sector)
                .WithMany()
                .HasForeignKey(cycle => cycle.SectorId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<SectorVolatilityCycle>()
                .HasIndex(cycle => new { cycle.SectorId, cycle.LastUpdatedAt })
                .IsUnique(false);

            // Configure CorporateWar
            modelBuilder.Entity<CorporateWar>()
                .HasKey(war => war.Id);
            modelBuilder.Entity<CorporateWar>()
                .HasOne(war => war.AttackerFaction)
                .WithMany()
                .HasForeignKey(war => war.AttackerFactionId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CorporateWar>()
                .HasOne(war => war.DefenderFaction)
                .WithMany()
                .HasForeignKey(war => war.DefenderFactionId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CorporateWar>()
                .HasIndex(war => new { war.IsActive, war.StartedAt })
                .IsUnique(false);

            // Configure InfrastructureOwnership
            modelBuilder.Entity<InfrastructureOwnership>()
                .HasKey(ownership => ownership.Id);
            modelBuilder.Entity<InfrastructureOwnership>()
                .HasOne(ownership => ownership.Sector)
                .WithMany()
                .HasForeignKey(ownership => ownership.SectorId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<InfrastructureOwnership>()
                .HasOne(ownership => ownership.Faction)
                .WithMany()
                .HasForeignKey(ownership => ownership.FactionId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<InfrastructureOwnership>()
                .HasIndex(ownership => new { ownership.SectorId, ownership.InfrastructureType })
                .IsUnique();

            // Configure TerritoryDominance
            modelBuilder.Entity<TerritoryDominance>()
                .HasKey(dominance => dominance.Id);
            modelBuilder.Entity<TerritoryDominance>()
                .HasOne(dominance => dominance.Faction)
                .WithMany()
                .HasForeignKey(dominance => dominance.FactionId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TerritoryDominance>()
                .HasIndex(dominance => dominance.FactionId)
                .IsUnique();

            // Configure InsurancePolicy
            modelBuilder.Entity<InsurancePolicy>()
                .HasKey(policy => policy.Id);
            modelBuilder.Entity<InsurancePolicy>()
                .HasOne(policy => policy.Player)
                .WithMany()
                .HasForeignKey(policy => policy.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<InsurancePolicy>()
                .HasOne(policy => policy.Ship)
                .WithMany()
                .HasForeignKey(policy => policy.ShipId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<InsurancePolicy>()
                .HasIndex(policy => policy.ShipId)
                .IsUnique();
            modelBuilder.Entity<InsurancePolicy>()
                .HasIndex(policy => new { policy.PlayerId, policy.IsActive })
                .IsUnique(false);

            // Configure InsuranceClaim
            modelBuilder.Entity<InsuranceClaim>()
                .HasKey(claim => claim.Id);
            modelBuilder.Entity<InsuranceClaim>()
                .HasOne(claim => claim.Policy)
                .WithMany()
                .HasForeignKey(claim => claim.PolicyId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<InsuranceClaim>()
                .HasOne(claim => claim.CombatLog)
                .WithMany()
                .HasForeignKey(claim => claim.CombatLogId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<InsuranceClaim>()
                .HasIndex(claim => new { claim.PolicyId, claim.FiledAt })
                .IsUnique(false);

            // Configure IntelligenceNetwork
            modelBuilder.Entity<IntelligenceNetwork>()
                .HasKey(network => network.Id);
            modelBuilder.Entity<IntelligenceNetwork>()
                .HasOne(network => network.OwnerPlayer)
                .WithMany()
                .HasForeignKey(network => network.OwnerPlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<IntelligenceNetwork>()
                .HasIndex(network => new { network.OwnerPlayerId, network.Name })
                .IsUnique();

            // Configure IntelligenceReport
            modelBuilder.Entity<IntelligenceReport>()
                .HasKey(report => report.Id);
            modelBuilder.Entity<IntelligenceReport>()
                .HasOne(report => report.Network)
                .WithMany(network => network.Reports)
                .HasForeignKey(report => report.NetworkId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<IntelligenceReport>()
                .HasOne(report => report.Sector)
                .WithMany()
                .HasForeignKey(report => report.SectorId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<IntelligenceReport>()
                .HasIndex(report => new { report.NetworkId, report.SectorId, report.DetectedAt })
                .IsUnique(false);

            // Configure NPCAgent
            modelBuilder.Entity<NPCAgent>()
                .HasKey(n => n.Id);

            // Configure ChannelMessage
            modelBuilder.Entity<ChannelMessage>()
                .HasKey(message => message.Id);
            modelBuilder.Entity<ChannelMessage>()
                .HasOne(message => message.Sender)
                .WithMany()
                .HasForeignKey(message => message.SenderId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ChannelMessage>()
                .HasIndex(message => new { message.ChannelType, message.ChannelKey, message.CreatedAt })
                .IsUnique(false);

            // Add composite indexes for common queries
            modelBuilder.Entity<TradeTransaction>()
                .HasIndex(t => new { t.PlayerId, t.CreatedAt })
                .IsUnique(false);

            modelBuilder.Entity<CombatLog>()
                .HasIndex(c => new { c.AttackerId, c.BattleStartedAt })
                .IsUnique(false);

            modelBuilder.Entity<MarketPriceHistory>()
                .HasIndex(mh => new { mh.MarketListingId, mh.RecordedAt })
                .IsUnique(false);

            // Configure TerraColonistSource
            modelBuilder.Entity<TerraColonistSource>()
                .HasKey(source => source.Id);
            modelBuilder.Entity<TerraColonistSource>()
                .HasOne(source => source.Sector)
                .WithMany()
                .HasForeignKey(source => source.SectorId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TerraColonistSource>()
                .HasIndex(source => source.SectorId)
                .IsUnique();

            // Configure ColonistShipment
            modelBuilder.Entity<ColonistShipment>()
                .HasKey(shipment => shipment.Id);
            modelBuilder.Entity<ColonistShipment>()
                .HasOne(shipment => shipment.Player)
                .WithMany()
                .HasForeignKey(shipment => shipment.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ColonistShipment>()
                .HasOne(shipment => shipment.FromSector)
                .WithMany()
                .HasForeignKey(shipment => shipment.FromSectorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ColonistShipment>()
                .HasOne(shipment => shipment.DestinationSector)
                .WithMany()
                .HasForeignKey(shipment => shipment.DestinationSectorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ColonistShipment>()
                .HasIndex(shipment => new { shipment.PlayerId, shipment.Status, shipment.EstimatedArrivalAtUtc })
                .IsUnique(false);

            // Configure ColonistDeliveryAudit
            modelBuilder.Entity<ColonistDeliveryAudit>()
                .HasKey(audit => audit.Id);
            modelBuilder.Entity<ColonistDeliveryAudit>()
                .HasOne(audit => audit.Shipment)
                .WithMany()
                .HasForeignKey(audit => audit.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ColonistDeliveryAudit>()
                .HasOne(audit => audit.Player)
                .WithMany()
                .HasForeignKey(audit => audit.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ColonistDeliveryAudit>()
                .HasOne(audit => audit.DestinationSector)
                .WithMany()
                .HasForeignKey(audit => audit.DestinationSectorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ColonistDeliveryAudit>()
                .HasIndex(audit => new { audit.PlayerId, audit.DeliveredAtUtc })
                .IsUnique(false);
        }
    }
}
