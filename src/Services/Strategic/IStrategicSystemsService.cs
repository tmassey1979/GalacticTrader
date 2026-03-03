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

    Task<IReadOnlyList<TerritoryEconomicPolicyDto>> GetTerritoryEconomicPoliciesAsync(
        Guid? factionId = null,
        CancellationToken cancellationToken = default);

    Task<TerritoryEconomicPolicyDto?> UpsertTerritoryEconomicPolicyAsync(
        UpsertTerritoryEconomicPolicyRequest request,
        CancellationToken cancellationToken = default);

    Task<InsurancePolicyDto?> UpsertInsurancePolicyAsync(
        UpsertInsurancePolicyRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InsurancePolicyDto>> GetInsurancePoliciesAsync(
        Guid playerId,
        CancellationToken cancellationToken = default);

    Task<InsuranceClaimDto?> FileInsuranceClaimAsync(
        FileInsuranceClaimRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InsuranceClaimDto>> GetInsuranceClaimsAsync(
        Guid playerId,
        CancellationToken cancellationToken = default);

    Task<IntelligenceNetworkDto?> CreateIntelligenceNetworkAsync(
        CreateIntelligenceNetworkRequest request,
        CancellationToken cancellationToken = default);

    Task<IntelligenceReportDto?> PublishIntelligenceReportAsync(
        PublishIntelligenceReportRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IntelligenceReportDto>> GetIntelligenceReportsAsync(
        Guid playerId,
        Guid? sectorId,
        CancellationToken cancellationToken = default);

    Task<int> ExpireIntelligenceReportsAsync(
        CancellationToken cancellationToken = default);
}
