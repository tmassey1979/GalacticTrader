namespace GalacticTrader.Services.Navigation;

public interface ISectorService
{
    Task<IEnumerable<SectorDto>> GetAllSectorsAsync(CancellationToken cancellationToken = default);
    Task<SectorDto?> GetSectorByIdAsync(Guid sectorId, CancellationToken cancellationToken = default);
    Task<SectorDto?> GetSectorByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<SectorDto>> GetSectorsByCoordsRangeAsync(
        float minX,
        float maxX,
        float minY,
        float maxY,
        float minZ,
        float maxZ,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<SectorDto>> GetSectorsByFactionAsync(Guid factionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SectorDto>> GetAdjacentSectorsAsync(Guid sectorId, CancellationToken cancellationToken = default);
    Task<SectorDto> CreateSectorAsync(string name, float x, float y, float z, CancellationToken cancellationToken = default);
    Task<SectorDto?> UpdateSectorAsync(
        Guid sectorId,
        int? securityLevel = null,
        int? hazardRating = null,
        Guid? factionId = null,
        CancellationToken cancellationToken = default);
    Task<bool> DeleteSectorAsync(Guid sectorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SectorDto>> GetHighSecuritySectorsAsync(int threshold = 70, CancellationToken cancellationToken = default);
    Task<IEnumerable<SectorDto>> GetHighRiskSectorsAsync(int threshold = 70, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetSecurityLevelDistributionAsync(CancellationToken cancellationToken = default);
}
