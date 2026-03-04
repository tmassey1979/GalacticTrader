using GalacticTrader.ClientSdk.Fleet;
using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Realtime;

namespace GalacticTrader.Tests;

public sealed class FleetModuleServiceTests
{
    [Fact]
    public async Task LoadStateAsync_BuildsFleetSummaryFromShipsAndEscort()
    {
        var playerId = Guid.NewGuid();
        var ships = new[]
        {
            new ShipApiDto
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                Name = "Astra",
                ShipClass = "Frigate",
                HullIntegrity = 80,
                MaxHullIntegrity = 100,
                CrewCount = 12,
                CrewSlots = 20,
                Hardpoints = 4,
                ShieldCapacity = 60,
                CurrentValue = 300_000m,
                HasInsurance = true
            },
            new ShipApiDto
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                Name = "Hawk",
                ShipClass = "Escort",
                HullIntegrity = 90,
                MaxHullIntegrity = 100,
                CrewCount = 8,
                CrewSlots = 12,
                Hardpoints = 5,
                ShieldCapacity = 80,
                CurrentValue = 420_000m,
                HasInsurance = false
            }
        };
        var templates = new[]
        {
            new ShipTemplateApiDto { Key = "escort", ShipClass = "Escort", PurchasePrice = 250_000m },
            new ShipTemplateApiDto { Key = "hauler", ShipClass = "Hauler", PurchasePrice = 400_000m }
        };
        var escort = new EscortSummaryApiDto
        {
            PlayerId = playerId,
            FleetStrength = 110,
            CoordinationBonus = 0.18f
        };
        var service = CreateService(templates: templates, ships: ships, escort: escort);

        var state = await service.LoadStateAsync(playerId);

        Assert.Equal(2, state.Summary.ShipCount);
        Assert.Equal(20, state.Summary.CrewCount);
        Assert.Equal(32, state.Summary.CrewCapacity);
        Assert.Equal(1, state.Summary.InsuredShipCount);
        Assert.Equal(720_000m, state.Summary.FleetValue);
        Assert.Equal(85d, state.Summary.AverageHullIntegrityPercent);
        Assert.Equal(110, state.Summary.FleetStrength);
        Assert.Equal("Guarded", state.Summary.ProtectionStatus);
    }

    [Fact]
    public async Task PurchaseShipAsync_ReturnsValidationFailure_ForMissingInputs()
    {
        var service = CreateService();

        var result = await service.PurchaseShipAsync(new PurchaseShipApiRequest
        {
            PlayerId = Guid.Empty,
            TemplateKey = "",
            Name = ""
        });

        Assert.False(result.Succeeded);
        Assert.Equal(FleetOperationFailureState.Validation, result.FailureState);
    }

    [Fact]
    public async Task ApplyRealtimeSnapshot_UpdatesFleetStrengthAndProtectionStatus()
    {
        var playerId = Guid.NewGuid();
        var service = CreateService(
            ships:
            [
                new ShipApiDto
                {
                    Id = Guid.NewGuid(),
                    PlayerId = playerId,
                    Name = "Atlas",
                    ShipClass = "Cruiser",
                    HullIntegrity = 100,
                    MaxHullIntegrity = 100,
                    CrewCount = 10,
                    CrewSlots = 15,
                    Hardpoints = 5,
                    ShieldCapacity = 90,
                    CurrentValue = 600_000m
                }
            ]);
        var state = await service.LoadStateAsync(playerId);
        var snapshot = new DashboardRealtimeSnapshotApiDto
        {
            CapturedAtUtc = new DateTime(2026, 3, 4, 9, 0, 0, DateTimeKind.Utc),
            Metrics = new DashboardRealtimeMetricsApiDto
            {
                FleetStrength = 188,
                ProtectionStatus = "Fortified"
            }
        };

        var projected = service.ApplyRealtimeSnapshot(state, snapshot);

        Assert.Equal(188, projected.Summary.FleetStrength);
        Assert.Equal("Fortified", projected.Summary.ProtectionStatus);
        Assert.Equal(snapshot.CapturedAtUtc, projected.LoadedAtUtc);
    }

    private static FleetModuleService CreateService(
        IReadOnlyList<ShipTemplateApiDto>? templates = null,
        IReadOnlyList<ShipApiDto>? ships = null,
        EscortSummaryApiDto? escort = null,
        Func<PurchaseShipApiRequest, ShipApiDto>? purchaseShip = null)
    {
        templates ??= [];
        ships ??= [];
        purchaseShip ??= request => new ShipApiDto
        {
            Id = Guid.NewGuid(),
            PlayerId = request.PlayerId,
            Name = request.Name,
            ShipClass = request.TemplateKey,
            MaxHullIntegrity = 100,
            HullIntegrity = 100
        };

        return new FleetModuleService(new FleetDataSource
        {
            LoadShipTemplatesAsync = _ => Task.FromResult(templates),
            LoadShipsAsync = (_, _) => Task.FromResult(ships),
            LoadEscortSummaryAsync = (_, _, _) => Task.FromResult<EscortSummaryApiDto?>(escort),
            PurchaseShipAsync = (request, _) => Task.FromResult(purchaseShip.Invoke(request)),
            SimulateConvoyAsync = (_, _) => Task.FromResult<ConvoySimulationResultApiDto?>(new ConvoySimulationResultApiDto())
        });
    }
}
