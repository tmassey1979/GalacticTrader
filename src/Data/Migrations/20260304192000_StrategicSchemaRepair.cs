using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GalacticTrader.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(global::GalacticTrader.Data.GalacticTraderDbContext))]
    [Migration("20260304192000_StrategicSchemaRepair")]
    public partial class StrategicSchemaRepair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "SectorVolatilityCycles" (
                    "Id" uuid NOT NULL,
                    "SectorId" uuid NOT NULL,
                    "CurrentPhase" text NOT NULL,
                    "VolatilityIndex" real NOT NULL,
                    "CycleStartedAt" timestamp with time zone NOT NULL,
                    "NextTransitionAt" timestamp with time zone NOT NULL,
                    "LastUpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_SectorVolatilityCycles" PRIMARY KEY ("Id")
                );

                CREATE TABLE IF NOT EXISTS "CorporateWars" (
                    "Id" uuid NOT NULL,
                    "AttackerFactionId" uuid NOT NULL,
                    "DefenderFactionId" uuid NOT NULL,
                    "CasusBelli" text NOT NULL,
                    "Intensity" integer NOT NULL,
                    "StartedAt" timestamp with time zone NOT NULL,
                    "EndedAt" timestamp with time zone NULL,
                    "IsActive" boolean NOT NULL,
                    CONSTRAINT "PK_CorporateWars" PRIMARY KEY ("Id")
                );

                CREATE TABLE IF NOT EXISTS "InfrastructureOwnerships" (
                    "Id" uuid NOT NULL,
                    "SectorId" uuid NOT NULL,
                    "FactionId" uuid NOT NULL,
                    "InfrastructureType" text NOT NULL,
                    "ControlScore" real NOT NULL,
                    "ClaimedAt" timestamp with time zone NOT NULL,
                    "LastUpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_InfrastructureOwnerships" PRIMARY KEY ("Id")
                );

                CREATE TABLE IF NOT EXISTS "TerritoryDominances" (
                    "Id" uuid NOT NULL,
                    "FactionId" uuid NOT NULL,
                    "ControlledSectorCount" integer NOT NULL,
                    "InfrastructureControlScore" real NOT NULL,
                    "WarMomentumScore" real NOT NULL,
                    "DominanceScore" real NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_TerritoryDominances" PRIMARY KEY ("Id")
                );

                CREATE TABLE IF NOT EXISTS "IntelligenceNetworks" (
                    "Id" uuid NOT NULL,
                    "OwnerPlayerId" uuid NOT NULL,
                    "Name" text NOT NULL,
                    "AssetCount" integer NOT NULL,
                    "CoverageScore" real NOT NULL,
                    "IsActive" boolean NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_IntelligenceNetworks" PRIMARY KEY ("Id")
                );

                CREATE TABLE IF NOT EXISTS "InsurancePolicies" (
                    "Id" uuid NOT NULL,
                    "PlayerId" uuid NOT NULL,
                    "ShipId" uuid NOT NULL,
                    "CoverageRate" real NOT NULL,
                    "PremiumPerCycle" numeric NOT NULL,
                    "RiskTier" text NOT NULL,
                    "IsActive" boolean NOT NULL,
                    "LastPremiumChargedAt" timestamp with time zone NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_InsurancePolicies" PRIMARY KEY ("Id")
                );

                CREATE TABLE IF NOT EXISTS "InsuranceClaims" (
                    "Id" uuid NOT NULL,
                    "PolicyId" uuid NOT NULL,
                    "CombatLogId" uuid NULL,
                    "ClaimAmount" numeric NOT NULL,
                    "FraudRiskScore" real NOT NULL,
                    "Status" text NOT NULL,
                    "FiledAt" timestamp with time zone NOT NULL,
                    "ResolvedAt" timestamp with time zone NULL,
                    CONSTRAINT "PK_InsuranceClaims" PRIMARY KEY ("Id")
                );

                CREATE TABLE IF NOT EXISTS "IntelligenceReports" (
                    "Id" uuid NOT NULL,
                    "NetworkId" uuid NOT NULL,
                    "SectorId" uuid NOT NULL,
                    "SignalType" text NOT NULL,
                    "ConfidenceScore" real NOT NULL,
                    "Payload" text NOT NULL,
                    "DetectedAt" timestamp with time zone NOT NULL,
                    "ExpiresAt" timestamp with time zone NOT NULL,
                    "IsExpired" boolean NOT NULL,
                    CONSTRAINT "PK_IntelligenceReports" PRIMARY KEY ("Id")
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_SectorVolatilityCycles_Sectors_SectorId') THEN
                        ALTER TABLE "SectorVolatilityCycles"
                            ADD CONSTRAINT "FK_SectorVolatilityCycles_Sectors_SectorId"
                            FOREIGN KEY ("SectorId") REFERENCES "Sectors" ("Id") ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_CorporateWars_Factions_AttackerFactionId') THEN
                        ALTER TABLE "CorporateWars"
                            ADD CONSTRAINT "FK_CorporateWars_Factions_AttackerFactionId"
                            FOREIGN KEY ("AttackerFactionId") REFERENCES "Factions" ("Id") ON DELETE RESTRICT;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_CorporateWars_Factions_DefenderFactionId') THEN
                        ALTER TABLE "CorporateWars"
                            ADD CONSTRAINT "FK_CorporateWars_Factions_DefenderFactionId"
                            FOREIGN KEY ("DefenderFactionId") REFERENCES "Factions" ("Id") ON DELETE RESTRICT;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_InfrastructureOwnerships_Factions_FactionId') THEN
                        ALTER TABLE "InfrastructureOwnerships"
                            ADD CONSTRAINT "FK_InfrastructureOwnerships_Factions_FactionId"
                            FOREIGN KEY ("FactionId") REFERENCES "Factions" ("Id") ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_InfrastructureOwnerships_Sectors_SectorId') THEN
                        ALTER TABLE "InfrastructureOwnerships"
                            ADD CONSTRAINT "FK_InfrastructureOwnerships_Sectors_SectorId"
                            FOREIGN KEY ("SectorId") REFERENCES "Sectors" ("Id") ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_TerritoryDominances_Factions_FactionId') THEN
                        ALTER TABLE "TerritoryDominances"
                            ADD CONSTRAINT "FK_TerritoryDominances_Factions_FactionId"
                            FOREIGN KEY ("FactionId") REFERENCES "Factions" ("Id") ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_IntelligenceNetworks_Players_OwnerPlayerId') THEN
                        ALTER TABLE "IntelligenceNetworks"
                            ADD CONSTRAINT "FK_IntelligenceNetworks_Players_OwnerPlayerId"
                            FOREIGN KEY ("OwnerPlayerId") REFERENCES "Players" ("Id") ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_InsurancePolicies_Players_PlayerId') THEN
                        ALTER TABLE "InsurancePolicies"
                            ADD CONSTRAINT "FK_InsurancePolicies_Players_PlayerId"
                            FOREIGN KEY ("PlayerId") REFERENCES "Players" ("Id") ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_InsurancePolicies_Ships_ShipId') THEN
                        ALTER TABLE "InsurancePolicies"
                            ADD CONSTRAINT "FK_InsurancePolicies_Ships_ShipId"
                            FOREIGN KEY ("ShipId") REFERENCES "Ships" ("Id") ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_InsuranceClaims_CombatLogs_CombatLogId') THEN
                        ALTER TABLE "InsuranceClaims"
                            ADD CONSTRAINT "FK_InsuranceClaims_CombatLogs_CombatLogId"
                            FOREIGN KEY ("CombatLogId") REFERENCES "CombatLogs" ("Id") ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_InsuranceClaims_InsurancePolicies_PolicyId') THEN
                        ALTER TABLE "InsuranceClaims"
                            ADD CONSTRAINT "FK_InsuranceClaims_InsurancePolicies_PolicyId"
                            FOREIGN KEY ("PolicyId") REFERENCES "InsurancePolicies" ("Id") ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_IntelligenceReports_IntelligenceNetworks_NetworkId') THEN
                        ALTER TABLE "IntelligenceReports"
                            ADD CONSTRAINT "FK_IntelligenceReports_IntelligenceNetworks_NetworkId"
                            FOREIGN KEY ("NetworkId") REFERENCES "IntelligenceNetworks" ("Id") ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_IntelligenceReports_Sectors_SectorId') THEN
                        ALTER TABLE "IntelligenceReports"
                            ADD CONSTRAINT "FK_IntelligenceReports_Sectors_SectorId"
                            FOREIGN KEY ("SectorId") REFERENCES "Sectors" ("Id") ON DELETE CASCADE;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS "IX_SectorVolatilityCycles_SectorId_LastUpdatedAt"
                    ON "SectorVolatilityCycles" ("SectorId", "LastUpdatedAt");

                CREATE INDEX IF NOT EXISTS "IX_CorporateWars_AttackerFactionId"
                    ON "CorporateWars" ("AttackerFactionId");
                CREATE INDEX IF NOT EXISTS "IX_CorporateWars_DefenderFactionId"
                    ON "CorporateWars" ("DefenderFactionId");
                CREATE INDEX IF NOT EXISTS "IX_CorporateWars_IsActive_StartedAt"
                    ON "CorporateWars" ("IsActive", "StartedAt");

                CREATE INDEX IF NOT EXISTS "IX_InfrastructureOwnerships_FactionId"
                    ON "InfrastructureOwnerships" ("FactionId");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_InfrastructureOwnerships_SectorId_InfrastructureType"
                    ON "InfrastructureOwnerships" ("SectorId", "InfrastructureType");

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_TerritoryDominances_FactionId"
                    ON "TerritoryDominances" ("FactionId");

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_IntelligenceNetworks_OwnerPlayerId_Name"
                    ON "IntelligenceNetworks" ("OwnerPlayerId", "Name");

                CREATE INDEX IF NOT EXISTS "IX_IntelligenceReports_NetworkId_SectorId_DetectedAt"
                    ON "IntelligenceReports" ("NetworkId", "SectorId", "DetectedAt");
                CREATE INDEX IF NOT EXISTS "IX_IntelligenceReports_SectorId"
                    ON "IntelligenceReports" ("SectorId");

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_InsurancePolicies_ShipId"
                    ON "InsurancePolicies" ("ShipId");
                CREATE INDEX IF NOT EXISTS "IX_InsurancePolicies_PlayerId_IsActive"
                    ON "InsurancePolicies" ("PlayerId", "IsActive");

                CREATE INDEX IF NOT EXISTS "IX_InsuranceClaims_CombatLogId"
                    ON "InsuranceClaims" ("CombatLogId");
                CREATE INDEX IF NOT EXISTS "IX_InsuranceClaims_PolicyId_FiledAt"
                    ON "InsuranceClaims" ("PolicyId", "FiledAt");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentional no-op: this migration repairs legacy schema drift.
        }
    }
}
