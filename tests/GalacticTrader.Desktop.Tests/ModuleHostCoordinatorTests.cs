using GalacticTrader.ClientSdk.Shell;

namespace GalacticTrader.Desktop.Tests;

public sealed class ModuleHostCoordinatorTests
{
    [Fact]
    public async Task SwitchAsync_ActivatesTargetModuleAndPublishesReadyState()
    {
        await using var host = new ModuleHostCoordinator();
        var dashboard = new FakeModule(GameplayModuleId.Dashboard);
        host.RegisterModule(dashboard);

        var result = await host.SwitchAsync(GameplayModuleId.Dashboard);

        Assert.True(result.Succeeded);
        Assert.Equal(GameplayModuleId.Dashboard, host.ActiveModuleId);
        Assert.Equal(1, dashboard.ActivatedCount);
        Assert.Equal(ModuleUxState.Ready, host.LastState?.State);
    }

    [Fact]
    public async Task SwitchAsync_DeactivatesCurrentModuleAndCancelsLifetimeBeforeCleanup()
    {
        await using var host = new ModuleHostCoordinator();
        var dashboard = new FakeModule(GameplayModuleId.Dashboard);
        var trading = new FakeModule(GameplayModuleId.Trading);
        host.RegisterModule(dashboard);
        host.RegisterModule(trading);

        await host.SwitchAsync(GameplayModuleId.Dashboard);
        var result = await host.SwitchAsync(GameplayModuleId.Trading);

        Assert.True(result.Succeeded);
        Assert.Equal(1, dashboard.DeactivatedCount);
        Assert.True(dashboard.LastActivationToken.IsCancellationRequested);
        Assert.Equal(1, trading.ActivatedCount);
        Assert.Equal(GameplayModuleId.Trading, host.ActiveModuleId);
    }

    [Fact]
    public async Task SwitchAsync_ReturnsErrorWhenModuleIsNotRegistered()
    {
        await using var host = new ModuleHostCoordinator();
        var result = await host.SwitchAsync(GameplayModuleId.Territory);

        Assert.False(result.Succeeded);
        Assert.Equal(ModuleUxState.Error, result.State);
        Assert.Null(host.ActiveModuleId);
    }

    [Fact]
    public async Task SwitchAsync_ReturnsNoOpWhenRequestedModuleIsAlreadyActive()
    {
        await using var host = new ModuleHostCoordinator();
        var dashboard = new FakeModule(GameplayModuleId.Dashboard);
        host.RegisterModule(dashboard);

        await host.SwitchAsync(GameplayModuleId.Dashboard);
        var result = await host.SwitchAsync(GameplayModuleId.Dashboard);

        Assert.True(result.Succeeded);
        Assert.True(result.NoOp);
        Assert.Equal(1, dashboard.ActivatedCount);
        Assert.Equal(0, dashboard.DeactivatedCount);
    }

    [Fact]
    public async Task SwitchAsync_ReportsErrorWhenActivationThrows()
    {
        await using var host = new ModuleHostCoordinator();
        host.RegisterModule(new ThrowingModule(GameplayModuleId.Battles));

        var result = await host.SwitchAsync(GameplayModuleId.Battles);

        Assert.False(result.Succeeded);
        Assert.Equal(ModuleUxState.Error, result.State);
        Assert.Null(host.ActiveModuleId);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public void HotkeyRouter_ResolvesGesturesCaseInsensitively()
    {
        var router = new ModuleHotkeyRouter(
        [
            new ModuleHotkeyBinding("Ctrl+1", GameplayModuleId.Dashboard),
            new ModuleHotkeyBinding("Ctrl+2", GameplayModuleId.Trading)
        ]);

        Assert.True(router.TryResolve("ctrl+1", out var module));
        Assert.Equal(GameplayModuleId.Dashboard, module);
        Assert.True(router.TryResolve("CTRL+2", out module));
        Assert.Equal(GameplayModuleId.Trading, module);
        Assert.False(router.TryResolve("ctrl+9", out _));
    }

    private sealed class FakeModule(GameplayModuleId moduleId) : IModuleLifecycleAdapter
    {
        public int ActivatedCount { get; private set; }

        public int DeactivatedCount { get; private set; }

        public GameplayModuleId ModuleId { get; } = moduleId;

        public CancellationToken LastActivationToken { get; private set; }

        public Task OnActivatedAsync(CancellationToken cancellationToken)
        {
            ActivatedCount++;
            LastActivationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task OnDeactivatedAsync(CancellationToken cancellationToken)
        {
            DeactivatedCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingModule(GameplayModuleId moduleId) : IModuleLifecycleAdapter
    {
        public GameplayModuleId ModuleId { get; } = moduleId;

        public Task OnActivatedAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Activation failed.");
        }

        public Task OnDeactivatedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
