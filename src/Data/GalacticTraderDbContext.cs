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

        // Leaderboards
        public DbSet<Leaderboard> Leaderboards { get; set; }

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

            // Configure NPCAgent
            modelBuilder.Entity<NPCAgent>()
                .HasKey(n => n.Id);

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
        }
    }
}
