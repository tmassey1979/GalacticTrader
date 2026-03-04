namespace GalacticTrader.ClientSdk.Shell;

public sealed class ModuleHostCoordinator : IAsyncDisposable
{
    private readonly Dictionary<GameplayModuleId, IModuleLifecycleAdapter> _modules = [];
    private readonly SemaphoreSlim _switchGate = new(1, 1);
    private CancellationTokenSource? _activeModuleLifetime;
    private bool _disposed;

    public GameplayModuleId? ActiveModuleId { get; private set; }

    public ModuleStateSnapshot? LastState { get; private set; }

    public event Action<ModuleStateSnapshot>? StateChanged;

    public void RegisterModule(IModuleLifecycleAdapter module)
    {
        ArgumentNullException.ThrowIfNull(module);
        ThrowIfDisposed();
        _modules[module.ModuleId] = module;
    }

    public bool TryGetModule(GameplayModuleId moduleId, out IModuleLifecycleAdapter? module)
    {
        ThrowIfDisposed();
        return _modules.TryGetValue(moduleId, out module);
    }

    public async Task<ModuleSwitchResult> SwitchAsync(
        GameplayModuleId moduleId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _switchGate.WaitAsync(cancellationToken);
        try
        {
            if (!_modules.TryGetValue(moduleId, out var requestedModule))
            {
                return ModuleSwitchResult.Failure(ModuleUxState.Error, $"Module '{moduleId}' is not registered.", ActiveModuleId);
            }

            if (ActiveModuleId == moduleId)
            {
                Publish(moduleId, ModuleUxState.Ready, "Module already active.");
                return ModuleSwitchResult.Success(moduleId, ModuleUxState.Ready, "Module already active.", noOp: true);
            }

            Publish(moduleId, ModuleUxState.Loading, "Loading module.");
            await DeactivateCurrentAsync(cancellationToken);

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _activeModuleLifetime = linkedCts;

            try
            {
                await requestedModule.OnActivatedAsync(linkedCts.Token);
                ActiveModuleId = moduleId;
                Publish(moduleId, ModuleUxState.Ready, "Module ready.");
                return ModuleSwitchResult.Success(moduleId, ModuleUxState.Ready, "Module switched.");
            }
            catch (Exception exception)
            {
                linkedCts.Cancel();
                linkedCts.Dispose();
                _activeModuleLifetime = null;
                ActiveModuleId = null;
                Publish(moduleId, ModuleUxState.Error, "Module failed to load.");
                return ModuleSwitchResult.Failure(ModuleUxState.Error, "Module failed to load.", exception: exception);
            }
        }
        finally
        {
            _switchGate.Release();
        }
    }

    public void MarkOffline(GameplayModuleId moduleId, string message)
    {
        ThrowIfDisposed();
        Publish(moduleId, ModuleUxState.Offline, message);
    }

    public void MarkError(GameplayModuleId moduleId, string message)
    {
        ThrowIfDisposed();
        Publish(moduleId, ModuleUxState.Error, message);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _switchGate.WaitAsync();
        try
        {
            await DeactivateCurrentAsync(CancellationToken.None);
        }
        finally
        {
            _switchGate.Release();
            _switchGate.Dispose();
        }
    }

    private async Task DeactivateCurrentAsync(CancellationToken cancellationToken)
    {
        if (ActiveModuleId is not { } activeModuleId)
        {
            _activeModuleLifetime?.Dispose();
            _activeModuleLifetime = null;
            return;
        }

        if (_modules.TryGetValue(activeModuleId, out var activeModule))
        {
            _activeModuleLifetime?.Cancel();
            await activeModule.OnDeactivatedAsync(cancellationToken);
        }

        _activeModuleLifetime?.Dispose();
        _activeModuleLifetime = null;
        ActiveModuleId = null;
    }

    private void Publish(GameplayModuleId moduleId, ModuleUxState state, string message)
    {
        var snapshot = new ModuleStateSnapshot(
            moduleId,
            state,
            message,
            DateTimeOffset.UtcNow);

        LastState = snapshot;
        StateChanged?.Invoke(snapshot);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
