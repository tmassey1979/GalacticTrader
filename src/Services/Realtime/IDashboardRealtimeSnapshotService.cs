namespace GalacticTrader.Services.Realtime;

public interface IDashboardRealtimeSnapshotService
{
    Task<DashboardRealtimeSnapshotDto> BuildSnapshotAsync(
        Guid playerId,
        int riskThreshold = 65,
        int transactionLimit = 25,
        int combatLimit = 25,
        CancellationToken cancellationToken = default);
}
