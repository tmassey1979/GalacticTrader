namespace GalacticTrader.Services.Strategic;

public interface IStrategicSystemsService
{
    Task<SectorVolatilityCycleDto?> UpsertSectorVolatilityCycleAsync(
        UpdateSectorVolatilityCycleRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SectorVolatilityCycleDto>> GetSectorVolatilityCyclesAsync(
        Guid? sectorId,
        CancellationToken cancellationToken = default);

    Task<CorporateWarDto?> DeclareCorporateWarAsync(
        DeclareCorporateWarRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CorporateWarDto>> GetCorporateWarsAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<InfrastructureOwnershipDto?> UpsertInfrastructureOwnershipAsync(
        UpdateInfrastructureOwnershipRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InfrastructureOwnershipDto>> GetInfrastructureOwnershipAsync(
        Guid? sectorId,
        CancellationToken cancellationToken = default);

    Task<TerritoryDominanceDto?> RecalculateTerritoryDominanceAsync(
        Guid factionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TerritoryDominanceDto>> GetTerritoryDominanceAsync(
        CancellationToken cancellationToken = default);
}
