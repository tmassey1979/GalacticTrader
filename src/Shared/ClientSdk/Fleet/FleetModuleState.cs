using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Fleet;

public sealed record FleetModuleState(
    IReadOnlyList<ShipTemplateApiDto> ShipTemplates,
    IReadOnlyList<ShipApiDto> Ships,
    EscortSummaryApiDto? EscortSummary,
    FleetStatusSummary Summary,
    DateTime LoadedAtUtc);
