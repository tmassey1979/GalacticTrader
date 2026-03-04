using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GalacticTrader.Data.Migrations
{
    /// <inheritdoc />
    public partial class TerraColonistLogisticsLoop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ColonistShipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromSectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationSectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ColonistCount = table.Column<long>(type: "bigint", nullable: false),
                    RouteTravelSeconds = table.Column<int>(type: "integer", nullable: false),
                    EstimatedRiskScore = table.Column<float>(type: "real", nullable: false),
                    TravelMode = table.Column<string>(type: "text", nullable: false),
                    LoadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EstimatedArrivalAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColonistShipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ColonistShipments_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ColonistShipments_Sectors_DestinationSectorId",
                        column: x => x.DestinationSectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ColonistShipments_Sectors_FromSectorId",
                        column: x => x.FromSectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TerraColonistSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailableColonists = table.Column<long>(type: "bigint", nullable: false),
                    OutputPerMinute = table.Column<int>(type: "integer", nullable: false),
                    StorageCapacity = table.Column<long>(type: "bigint", nullable: false),
                    LastGeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerraColonistSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TerraColonistSources_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ColonistDeliveryAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationSectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ColonistCount = table.Column<long>(type: "bigint", nullable: false),
                    EstimatedRiskScore = table.Column<float>(type: "real", nullable: false),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColonistDeliveryAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ColonistDeliveryAudits_ColonistShipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "ColonistShipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ColonistDeliveryAudits_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ColonistDeliveryAudits_Sectors_DestinationSectorId",
                        column: x => x.DestinationSectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ColonistDeliveryAudits_DestinationSectorId",
                table: "ColonistDeliveryAudits",
                column: "DestinationSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_ColonistDeliveryAudits_PlayerId_DeliveredAtUtc",
                table: "ColonistDeliveryAudits",
                columns: new[] { "PlayerId", "DeliveredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ColonistDeliveryAudits_ShipmentId",
                table: "ColonistDeliveryAudits",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ColonistShipments_DestinationSectorId",
                table: "ColonistShipments",
                column: "DestinationSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_ColonistShipments_FromSectorId",
                table: "ColonistShipments",
                column: "FromSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_ColonistShipments_PlayerId_Status_EstimatedArrivalAtUtc",
                table: "ColonistShipments",
                columns: new[] { "PlayerId", "Status", "EstimatedArrivalAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TerraColonistSources_SectorId",
                table: "TerraColonistSources",
                column: "SectorId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ColonistDeliveryAudits");

            migrationBuilder.DropTable(
                name: "TerraColonistSources");

            migrationBuilder.DropTable(
                name: "ColonistShipments");
        }
    }
}
