namespace GalacticTrader.ClientSdk.Starmap;

public sealed record StarmapFramePlan(
    StarmapChunkKey CenterChunk,
    IReadOnlyList<StarmapChunkKey> ActiveChunks,
    IReadOnlyList<StarmapSectorRenderPlan> Sectors,
    IReadOnlyList<StarmapRouteRenderPlan> Routes,
    bool WasSectorBudgetApplied,
    bool WasRouteBudgetApplied);
