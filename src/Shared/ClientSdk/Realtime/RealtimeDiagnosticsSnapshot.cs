namespace GalacticTrader.ClientSdk.Realtime;

public sealed record RealtimeDiagnosticsSnapshot(
    bool IsRunning,
    Guid? PlayerId,
    long StrategicSnapshotCount,
    long CommunicationMessageCount,
    long StrategicFaultCount,
    long CommunicationFaultCount,
    DateTimeOffset? LastStrategicMessageAtUtc,
    DateTimeOffset? LastCommunicationMessageAtUtc,
    DateTimeOffset? LastFaultAtUtc);
