namespace GalacticTrader.Services.Fleet;

public interface IFleetService
{
    Task<IReadOnlyList<ShipTemplateDto>> GetShipTemplatesAsync(CancellationToken cancellationToken = default);
    Task<ShipDto?> PurchaseShipAsync(PurchaseShipRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShipDto>> GetPlayerShipsAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<ShipDto?> GetShipAsync(Guid shipId, CancellationToken cancellationToken = default);
    Task<ShipDto?> InstallModuleAsync(InstallShipModuleRequest request, CancellationToken cancellationToken = default);

    Task<CrewMemberDto?> HireCrewAsync(HireCrewRequest request, CancellationToken cancellationToken = default);
    Task<CrewMemberDto?> ProgressCrewAsync(Guid crewId, CrewProgressRequest request, CancellationToken cancellationToken = default);
    Task<bool> FireCrewAsync(Guid crewId, CancellationToken cancellationToken = default);

    Task<EscortSummaryDto?> GetEscortSummaryAsync(Guid playerId, FleetFormation formation = FleetFormation.Defensive, CancellationToken cancellationToken = default);
    Task<ConvoySimulationResult?> SimulateConvoyAsync(ConvoySimulationRequest request, CancellationToken cancellationToken = default);
}
