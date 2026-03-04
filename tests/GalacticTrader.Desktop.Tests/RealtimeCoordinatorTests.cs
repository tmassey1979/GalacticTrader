using GalacticTrader.ClientSdk.Realtime;
using GalacticTrader.Desktop.Realtime;

namespace GalacticTrader.Desktop.Tests;

public sealed class RealtimeCoordinatorTests
{
    [Fact]
    public async Task StartAsync_ForSamePlayer_IsIdempotentAndAvoidsDuplicateHandlers()
    {
        var strategic = new FakeStrategicClient();
        var communication = new FakeCommunicationClient();
        await using var coordinator = new RealtimeCoordinator(strategic, communication);
        var playerId = Guid.NewGuid();
        var snapshotCount = 0;
        coordinator.StrategicSnapshotReceived += _ => snapshotCount++;

        await coordinator.StartAsync(playerId);
        await coordinator.StartAsync(playerId);
        strategic.EmitSnapshot(new DashboardRealtimeSnapshotApiDto());

        Assert.Equal(1, strategic.StartCallCount);
        Assert.Equal(1, communication.StartCallCount);
        Assert.Equal(1, snapshotCount);
    }

    [Fact]
    public async Task StartAsync_ForDifferentPlayer_RestartsUnderlyingClients()
    {
        var strategic = new FakeStrategicClient();
        var communication = new FakeCommunicationClient();
        await using var coordinator = new RealtimeCoordinator(strategic, communication);

        await coordinator.StartAsync(Guid.NewGuid());
        await coordinator.StartAsync(Guid.NewGuid());

        Assert.Equal(2, strategic.StartCallCount);
        Assert.Equal(2, communication.StartCallCount);
        Assert.Equal(1, strategic.StopCallCount);
        Assert.Equal(1, communication.StopCallCount);
    }

    [Fact]
    public async Task Diagnostics_TracksMessagesAndFaults()
    {
        var strategic = new FakeStrategicClient();
        var communication = new FakeCommunicationClient();
        await using var coordinator = new RealtimeCoordinator(strategic, communication);
        await coordinator.StartAsync(Guid.NewGuid());

        strategic.EmitSnapshot(new DashboardRealtimeSnapshotApiDto());
        communication.EmitMessage(new CommunicationRealtimeMessageApiDto());
        strategic.EmitFault(new InvalidOperationException("strategic fault"));
        communication.EmitFault(new InvalidOperationException("communication fault"));

        var diagnostics = coordinator.Diagnostics;
        Assert.Equal(1, diagnostics.StrategicSnapshotCount);
        Assert.Equal(1, diagnostics.CommunicationMessageCount);
        Assert.Equal(1, diagnostics.StrategicFaultCount);
        Assert.Equal(1, diagnostics.CommunicationFaultCount);
        Assert.NotNull(diagnostics.LastStrategicMessageAtUtc);
        Assert.NotNull(diagnostics.LastCommunicationMessageAtUtc);
        Assert.NotNull(diagnostics.LastFaultAtUtc);
    }

    [Fact]
    public async Task StopAsync_MarksCoordinatorNotRunning_AndIgnoresLateEvents()
    {
        var strategic = new FakeStrategicClient();
        var communication = new FakeCommunicationClient();
        await using var coordinator = new RealtimeCoordinator(strategic, communication);
        await coordinator.StartAsync(Guid.NewGuid());
        await coordinator.StopAsync();

        strategic.EmitSnapshot(new DashboardRealtimeSnapshotApiDto());
        communication.EmitMessage(new CommunicationRealtimeMessageApiDto());

        Assert.False(coordinator.Diagnostics.IsRunning);
        Assert.Equal(0, coordinator.Diagnostics.StrategicSnapshotCount);
        Assert.Equal(0, coordinator.Diagnostics.CommunicationMessageCount);
    }

    private sealed class FakeStrategicClient : IRealtimeStrategicClient
    {
        public int StartCallCount { get; private set; }

        public int StopCallCount { get; private set; }

        public event Action<DashboardRealtimeSnapshotApiDto>? SnapshotReceived;

        public event Action<Exception>? ConnectionFaulted;

        public void Start(Guid playerId, int intervalSeconds = 5)
        {
            StartCallCount++;
        }

        public Task StopAsync()
        {
            StopCallCount++;
            return Task.CompletedTask;
        }

        public void EmitSnapshot(DashboardRealtimeSnapshotApiDto snapshot)
        {
            SnapshotReceived?.Invoke(snapshot);
        }

        public void EmitFault(Exception exception)
        {
            ConnectionFaulted?.Invoke(exception);
        }
    }

    private sealed class FakeCommunicationClient : IRealtimeCommunicationClient
    {
        public int StartCallCount { get; private set; }

        public int StopCallCount { get; private set; }

        public event Action<CommunicationRealtimeMessageApiDto>? MessageReceived;

        public event Action<Exception>? ConnectionFaulted;

        public void Start(Guid playerId, string channelType = "global", string channelKey = "desktop-feed")
        {
            StartCallCount++;
        }

        public Task StopAsync()
        {
            StopCallCount++;
            return Task.CompletedTask;
        }

        public void EmitMessage(CommunicationRealtimeMessageApiDto message)
        {
            MessageReceived?.Invoke(message);
        }

        public void EmitFault(Exception exception)
        {
            ConnectionFaulted?.Invoke(exception);
        }
    }
}
