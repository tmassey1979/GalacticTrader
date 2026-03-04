using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GalacticTrader.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchemaBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Commodities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Volume = table.Column<float>(type: "real", nullable: false),
                    BasePrice = table.Column<float>(type: "real", nullable: false),
                    LegalityFactor = table.Column<float>(type: "real", nullable: false),
                    Rarity = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commodities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Factions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    AlignmentBias = table.Column<int>(type: "integer", nullable: false),
                    InfluenceScore = table.Column<float>(type: "real", nullable: false),
                    WealthScore = table.Column<float>(type: "real", nullable: false),
                    PowerScore = table.Column<float>(type: "real", nullable: false),
                    ReputationMultiplier = table.Column<double>(type: "double precision", nullable: false),
                    ReputationDecayPerDay = table.Column<int>(type: "integer", nullable: false),
                    ControlledSectors = table.Column<int>(type: "integer", nullable: false),
                    TreasuryBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    TradeGoodModifier = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    KeycloakUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NetWorth = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LiquidCredits = table.Column<decimal>(type: "numeric", nullable: false),
                    ReputationScore = table.Column<int>(type: "integer", nullable: false),
                    AlignmentLevel = table.Column<int>(type: "integer", nullable: false),
                    FleetStrengthRating = table.Column<int>(type: "integer", nullable: false),
                    ProtectionStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CorporateWars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttackerFactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenderFactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CasusBelli = table.Column<string>(type: "text", nullable: false),
                    Intensity = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorporateWars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorporateWars_Factions_AttackerFactionId",
                        column: x => x.AttackerFactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CorporateWars_Factions_DefenderFactionId",
                        column: x => x.DefenderFactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false),
                    SecurityLevel = table.Column<int>(type: "integer", nullable: false),
                    HazardRating = table.Column<int>(type: "integer", nullable: false),
                    ResourceModifier = table.Column<float>(type: "real", nullable: false),
                    EconomicIndex = table.Column<int>(type: "integer", nullable: false),
                    SensorInterferenceLevel = table.Column<float>(type: "real", nullable: false),
                    ControlledByFactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AverageTrafficLevel = table.Column<int>(type: "integer", nullable: false),
                    PiratePresenceProbability = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sectors_Factions_ControlledByFactionId",
                        column: x => x.ControlledByFactionId,
                        principalTable: "Factions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TerritoryDominances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ControlledSectorCount = table.Column<int>(type: "integer", nullable: false),
                    InfrastructureControlScore = table.Column<float>(type: "real", nullable: false),
                    WarMomentumScore = table.Column<float>(type: "real", nullable: false),
                    DominanceScore = table.Column<float>(type: "real", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerritoryDominances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TerritoryDominances_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelType = table.Column<string>(type: "text", nullable: false),
                    ChannelKey = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsModerated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelMessages_Players_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntelligenceNetworks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AssetCount = table.Column<int>(type: "integer", nullable: false),
                    CoverageScore = table.Column<float>(type: "real", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntelligenceNetworks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntelligenceNetworks_Players_OwnerPlayerId",
                        column: x => x.OwnerPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Leaderboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaderboardType = table.Column<string>(type: "text", nullable: false),
                    Rank = table.Column<long>(type: "bigint", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: false),
                    PreviousScore = table.Column<decimal>(type: "numeric", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaderboards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leaderboards_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerFactionRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReputationScore = table.Column<int>(type: "integer", nullable: false),
                    HasAccess = table.Column<bool>(type: "boolean", nullable: false),
                    TradingDiscount = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerFactionRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerFactionRelationships_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerFactionRelationships_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    KeycloakId = table.Column<string>(type: "text", nullable: false),
                    Roles = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccounts_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InfrastructureOwnerships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    FactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InfrastructureType = table.Column<string>(type: "text", nullable: false),
                    ControlScore = table.Column<float>(type: "real", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfrastructureOwnerships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InfrastructureOwnerships_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InfrastructureOwnerships_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Markets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Markets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Markets_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NPCAgents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Archetype = table.Column<string>(type: "text", nullable: false),
                    FactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WealthTarget = table.Column<float>(type: "real", nullable: false),
                    RiskTolerance = table.Column<float>(type: "real", nullable: false),
                    AggressionIndex = table.Column<int>(type: "integer", nullable: false),
                    Wealth = table.Column<decimal>(type: "numeric", nullable: false),
                    ReputationScore = table.Column<int>(type: "integer", nullable: false),
                    InfluenceScore = table.Column<float>(type: "real", nullable: false),
                    FleetSize = table.Column<int>(type: "integer", nullable: false),
                    CurrentLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentGoal = table.Column<string>(type: "text", nullable: false),
                    DecisionTick = table.Column<int>(type: "integer", nullable: false),
                    TradeVolume24h = table.Column<decimal>(type: "numeric", nullable: false),
                    TradesLegally = table.Column<bool>(type: "boolean", nullable: false),
                    TradesIllegally = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NPCAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NPCAgents_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NPCAgents_Sectors_CurrentLocationId",
                        column: x => x.CurrentLocationId,
                        principalTable: "Sectors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NPCAgents_Sectors_TargetLocationId",
                        column: x => x.TargetLocationId,
                        principalTable: "Sectors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromSectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToSectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravelTimeSeconds = table.Column<int>(type: "integer", nullable: false),
                    FuelCost = table.Column<float>(type: "real", nullable: false),
                    BaseRiskScore = table.Column<float>(type: "real", nullable: false),
                    VisibilityRating = table.Column<float>(type: "real", nullable: false),
                    LegalStatus = table.Column<string>(type: "text", nullable: false),
                    WarpGateType = table.Column<string>(type: "text", nullable: false),
                    IsDiscovered = table.Column<bool>(type: "boolean", nullable: false),
                    HasAnomalies = table.Column<bool>(type: "boolean", nullable: false),
                    TrafficIntensity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_Sectors_FromSectorId",
                        column: x => x.FromSectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Routes_Sectors_ToSectorId",
                        column: x => x.ToSectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SectorVolatilityCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentPhase = table.Column<string>(type: "text", nullable: false),
                    VolatilityIndex = table.Column<float>(type: "real", nullable: false),
                    CycleStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextTransitionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectorVolatilityCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectorVolatilityCycles_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ShipClass = table.Column<string>(type: "text", nullable: false),
                    HullIntegrity = table.Column<int>(type: "integer", nullable: false),
                    MaxHullIntegrity = table.Column<int>(type: "integer", nullable: false),
                    ShieldCapacity = table.Column<int>(type: "integer", nullable: false),
                    MaxShieldCapacity = table.Column<int>(type: "integer", nullable: false),
                    ReactorOutput = table.Column<int>(type: "integer", nullable: false),
                    CargoCapacity = table.Column<int>(type: "integer", nullable: false),
                    CargoUsed = table.Column<int>(type: "integer", nullable: false),
                    SensorRange = table.Column<int>(type: "integer", nullable: false),
                    SignatureProfile = table.Column<int>(type: "integer", nullable: false),
                    CrewSlots = table.Column<int>(type: "integer", nullable: false),
                    Hardpoints = table.Column<int>(type: "integer", nullable: false),
                    HasInsurance = table.Column<bool>(type: "boolean", nullable: false),
                    InsuranceRate = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsInCombat = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentSectorId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetSectorId = table.Column<Guid>(type: "uuid", nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ships_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ships_Sectors_CurrentSectorId",
                        column: x => x.CurrentSectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Ships_Sectors_TargetSectorId",
                        column: x => x.TargetSectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IntelligenceReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignalType = table.Column<string>(type: "text", nullable: false),
                    ConfidenceScore = table.Column<float>(type: "real", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsExpired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntelligenceReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntelligenceReports_IntelligenceNetworks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "IntelligenceNetworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntelligenceReports_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommodityId = table.Column<Guid>(type: "uuid", nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    DemandMultiplier = table.Column<float>(type: "real", nullable: false),
                    RiskPremium = table.Column<float>(type: "real", nullable: false),
                    ScarcityModifier = table.Column<float>(type: "real", nullable: false),
                    AvailableQuantity = table.Column<long>(type: "bigint", nullable: false),
                    MaxQuantity = table.Column<long>(type: "bigint", nullable: false),
                    MinQuantity = table.Column<long>(type: "bigint", nullable: false),
                    TotalTradeVolume24h = table.Column<long>(type: "bigint", nullable: false),
                    PriceChangePercent24h = table.Column<float>(type: "real", nullable: false),
                    PriceLastChanged = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketListings_Commodities_CommodityId",
                        column: x => x.CommodityId,
                        principalTable: "Commodities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketListings_Markets_MarketId",
                        column: x => x.MarketId,
                        principalTable: "Markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TradeTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommodityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromMarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToMarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<long>(type: "bigint", nullable: false),
                    PricePerUnit = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Tariff = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TransactionFee = table.Column<decimal>(type: "numeric", nullable: false),
                    InsuranceCost = table.Column<decimal>(type: "numeric", nullable: false),
                    NetProfit = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    UsedSmugglingRoute = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeTransactions_Commodities_CommodityId",
                        column: x => x.CommodityId,
                        principalTable: "Commodities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeTransactions_Markets_FromMarketId",
                        column: x => x.FromMarketId,
                        principalTable: "Markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeTransactions_Markets_ToMarketId",
                        column: x => x.ToMarketId,
                        principalTable: "Markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeTransactions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NPCShips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NPCAgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ShipClass = table.Column<string>(type: "text", nullable: false),
                    HullIntegrity = table.Column<int>(type: "integer", nullable: false),
                    MaxHullIntegrity = table.Column<int>(type: "integer", nullable: false),
                    CombatRating = table.Column<int>(type: "integer", nullable: false),
                    CurrentSectorId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NPCShips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NPCShips_NPCAgents_NPCAgentId",
                        column: x => x.NPCAgentId,
                        principalTable: "NPCAgents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NPCShips_Sectors_CurrentSectorId",
                        column: x => x.CurrentSectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Cargo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommodityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<long>(type: "bigint", nullable: false),
                    ValuePerUnit = table.Column<decimal>(type: "numeric", nullable: false),
                    LoadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cargo_Commodities_CommodityId",
                        column: x => x.CommodityId,
                        principalTable: "Commodities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cargo_Ships_ShipId",
                        column: x => x.ShipId,
                        principalTable: "Ships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CombatLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttackerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenderId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationSectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttackerShipId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenderShipId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttackerInitialRating = table.Column<int>(type: "integer", nullable: false),
                    DefenderInitialRating = table.Column<int>(type: "integer", nullable: false),
                    BattleOutcome = table.Column<string>(type: "text", nullable: false),
                    AttackerDamageDealt = table.Column<int>(type: "integer", nullable: false),
                    DefenderDamageDealt = table.Column<int>(type: "integer", nullable: false),
                    AttackerHullDamage = table.Column<int>(type: "integer", nullable: false),
                    DefenderHullDamage = table.Column<int>(type: "integer", nullable: false),
                    AttackerReward = table.Column<decimal>(type: "numeric", nullable: false),
                    DefenderCompensation = table.Column<decimal>(type: "numeric", nullable: false),
                    InsurancePayout = table.Column<decimal>(type: "numeric", nullable: false),
                    AttackerReputationChange = table.Column<int>(type: "integer", nullable: false),
                    DefenderReputationChange = table.Column<int>(type: "integer", nullable: false),
                    BattleStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BattleEndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    TotalTicks = table.Column<int>(type: "integer", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombatLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombatLogs_Players_AttackerId",
                        column: x => x.AttackerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CombatLogs_Players_DefenderId",
                        column: x => x.DefenderId,
                        principalTable: "Players",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CombatLogs_Sectors_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CombatLogs_Ships_AttackerShipId",
                        column: x => x.AttackerShipId,
                        principalTable: "Ships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CombatLogs_Ships_DefenderShipId",
                        column: x => x.DefenderShipId,
                        principalTable: "Ships",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Crew",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    CombatSkill = table.Column<int>(type: "integer", nullable: false),
                    EngineeringSkill = table.Column<int>(type: "integer", nullable: false),
                    NavigationSkill = table.Column<int>(type: "integer", nullable: false),
                    Morale = table.Column<int>(type: "integer", nullable: false),
                    Loyalty = table.Column<int>(type: "integer", nullable: false),
                    Salary = table.Column<decimal>(type: "numeric", nullable: false),
                    ExperienceLevel = table.Column<int>(type: "integer", nullable: false),
                    ExperiencePoints = table.Column<long>(type: "bigint", nullable: false),
                    HiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Crew", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Crew_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Crew_Ships_ShipId",
                        column: x => x.ShipId,
                        principalTable: "Ships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InsurancePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoverageRate = table.Column<float>(type: "real", nullable: false),
                    PremiumPerCycle = table.Column<decimal>(type: "numeric", nullable: false),
                    RiskTier = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastPremiumChargedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsurancePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsurancePolicies_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InsurancePolicies_Ships_ShipId",
                        column: x => x.ShipId,
                        principalTable: "Ships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShipModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleType = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    HealthPoints = table.Column<int>(type: "integer", nullable: false),
                    MaxHealthPoints = table.Column<int>(type: "integer", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    InstalledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipModules_Ships_ShipId",
                        column: x => x.ShipId,
                        principalTable: "Ships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketPriceHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Quantity = table.Column<long>(type: "bigint", nullable: false),
                    VolumeTraded = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPriceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketPriceHistories_MarketListings_MarketListingId",
                        column: x => x.MarketListingId,
                        principalTable: "MarketListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CombatLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClaimAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    FraudRiskScore = table.Column<float>(type: "real", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FiledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_CombatLogs_CombatLogId",
                        column: x => x.CombatLogId,
                        principalTable: "CombatLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_InsurancePolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "InsurancePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cargo_CommodityId",
                table: "Cargo",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_Cargo_ShipId",
                table: "Cargo",
                column: "ShipId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMessages_ChannelType_ChannelKey_CreatedAt",
                table: "ChannelMessages",
                columns: new[] { "ChannelType", "ChannelKey", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMessages_SenderId",
                table: "ChannelMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatLogs_AttackerId_BattleStartedAt",
                table: "CombatLogs",
                columns: new[] { "AttackerId", "BattleStartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CombatLogs_AttackerShipId",
                table: "CombatLogs",
                column: "AttackerShipId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatLogs_DefenderId",
                table: "CombatLogs",
                column: "DefenderId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatLogs_DefenderShipId",
                table: "CombatLogs",
                column: "DefenderShipId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatLogs_LocationId",
                table: "CombatLogs",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Commodities_Name",
                table: "Commodities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorporateWars_AttackerFactionId",
                table: "CorporateWars",
                column: "AttackerFactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CorporateWars_DefenderFactionId",
                table: "CorporateWars",
                column: "DefenderFactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CorporateWars_IsActive_StartedAt",
                table: "CorporateWars",
                columns: new[] { "IsActive", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Crew_PlayerId",
                table: "Crew",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Crew_ShipId",
                table: "Crew",
                column: "ShipId");

            migrationBuilder.CreateIndex(
                name: "IX_Factions_Name",
                table: "Factions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InfrastructureOwnerships_FactionId",
                table: "InfrastructureOwnerships",
                column: "FactionId");

            migrationBuilder.CreateIndex(
                name: "IX_InfrastructureOwnerships_SectorId_InfrastructureType",
                table: "InfrastructureOwnerships",
                columns: new[] { "SectorId", "InfrastructureType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_CombatLogId",
                table: "InsuranceClaims",
                column: "CombatLogId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_PolicyId_FiledAt",
                table: "InsuranceClaims",
                columns: new[] { "PolicyId", "FiledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_PlayerId_IsActive",
                table: "InsurancePolicies",
                columns: new[] { "PlayerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePolicies_ShipId",
                table: "InsurancePolicies",
                column: "ShipId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceNetworks_OwnerPlayerId_Name",
                table: "IntelligenceNetworks",
                columns: new[] { "OwnerPlayerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceReports_NetworkId_SectorId_DetectedAt",
                table: "IntelligenceReports",
                columns: new[] { "NetworkId", "SectorId", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceReports_SectorId",
                table: "IntelligenceReports",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_LeaderboardType_Rank",
                table: "Leaderboards",
                columns: new[] { "LeaderboardType", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_PlayerId",
                table: "Leaderboards",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListings_CommodityId",
                table: "MarketListings",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListings_MarketId",
                table: "MarketListings",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPriceHistories_MarketListingId_RecordedAt",
                table: "MarketPriceHistories",
                columns: new[] { "MarketListingId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Markets_SectorId",
                table: "Markets",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_NPCAgents_CurrentLocationId",
                table: "NPCAgents",
                column: "CurrentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_NPCAgents_FactionId",
                table: "NPCAgents",
                column: "FactionId");

            migrationBuilder.CreateIndex(
                name: "IX_NPCAgents_TargetLocationId",
                table: "NPCAgents",
                column: "TargetLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_NPCShips_CurrentSectorId",
                table: "NPCShips",
                column: "CurrentSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_NPCShips_NPCAgentId",
                table: "NPCShips",
                column: "NPCAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFactionRelationships_FactionId",
                table: "PlayerFactionRelationships",
                column: "FactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFactionRelationships_PlayerId_FactionId",
                table: "PlayerFactionRelationships",
                columns: new[] { "PlayerId", "FactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_KeycloakUserId",
                table: "Players",
                column: "KeycloakUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_Username",
                table: "Players",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_FromSectorId",
                table: "Routes",
                column: "FromSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ToSectorId",
                table: "Routes",
                column: "ToSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_ControlledByFactionId",
                table: "Sectors",
                column: "ControlledByFactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_Name",
                table: "Sectors",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SectorVolatilityCycles_SectorId_LastUpdatedAt",
                table: "SectorVolatilityCycles",
                columns: new[] { "SectorId", "LastUpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipModules_ShipId",
                table: "ShipModules",
                column: "ShipId");

            migrationBuilder.CreateIndex(
                name: "IX_Ships_CurrentSectorId",
                table: "Ships",
                column: "CurrentSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Ships_PlayerId",
                table: "Ships",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Ships_TargetSectorId",
                table: "Ships",
                column: "TargetSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_TerritoryDominances_FactionId",
                table: "TerritoryDominances",
                column: "FactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradeTransactions_CommodityId",
                table: "TradeTransactions",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeTransactions_FromMarketId",
                table: "TradeTransactions",
                column: "FromMarketId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeTransactions_PlayerId_CreatedAt",
                table: "TradeTransactions",
                columns: new[] { "PlayerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TradeTransactions_ToMarketId",
                table: "TradeTransactions",
                column: "ToMarketId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Email",
                table: "UserAccounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_KeycloakId",
                table: "UserAccounts",
                column: "KeycloakId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_PlayerId",
                table: "UserAccounts",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Username",
                table: "UserAccounts",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cargo");

            migrationBuilder.DropTable(
                name: "ChannelMessages");

            migrationBuilder.DropTable(
                name: "CorporateWars");

            migrationBuilder.DropTable(
                name: "Crew");

            migrationBuilder.DropTable(
                name: "InfrastructureOwnerships");

            migrationBuilder.DropTable(
                name: "InsuranceClaims");

            migrationBuilder.DropTable(
                name: "IntelligenceReports");

            migrationBuilder.DropTable(
                name: "Leaderboards");

            migrationBuilder.DropTable(
                name: "MarketPriceHistories");

            migrationBuilder.DropTable(
                name: "NPCShips");

            migrationBuilder.DropTable(
                name: "PlayerFactionRelationships");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "SectorVolatilityCycles");

            migrationBuilder.DropTable(
                name: "ShipModules");

            migrationBuilder.DropTable(
                name: "TerritoryDominances");

            migrationBuilder.DropTable(
                name: "TradeTransactions");

            migrationBuilder.DropTable(
                name: "UserAccounts");

            migrationBuilder.DropTable(
                name: "CombatLogs");

            migrationBuilder.DropTable(
                name: "InsurancePolicies");

            migrationBuilder.DropTable(
                name: "IntelligenceNetworks");

            migrationBuilder.DropTable(
                name: "MarketListings");

            migrationBuilder.DropTable(
                name: "NPCAgents");

            migrationBuilder.DropTable(
                name: "Ships");

            migrationBuilder.DropTable(
                name: "Commodities");

            migrationBuilder.DropTable(
                name: "Markets");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Sectors");

            migrationBuilder.DropTable(
                name: "Factions");
        }
    }
}
