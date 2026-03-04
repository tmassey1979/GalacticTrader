using GalacticTrader.ClientSdk.Shell;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GalacticTrader.Unity.Shell;

public sealed class UnityModuleHostController : MonoBehaviour
{
    [SerializeField] private GameplayModuleId startupModule = GameplayModuleId.Dashboard;
    [SerializeField] private ModuleUxStateOverlayController? uxStateOverlay;
    [SerializeField] private UnityShellModule[] modules = [];

    private ModuleHostCoordinator? _hostCoordinator;

    public GameplayModuleId? ActiveModuleId => _hostCoordinator?.ActiveModuleId;

    public event Action<ModuleStateSnapshot>? ModuleStateChanged;

    private async void Start()
    {
        _hostCoordinator = new ModuleHostCoordinator();
        _hostCoordinator.StateChanged += OnModuleStateChanged;

        foreach (var module in modules)
        {
            if (module is null)
            {
                continue;
            }

            _hostCoordinator.RegisterModule(module);
            module.gameObject.SetActive(false);
        }

        await SwitchToAsync(startupModule);
    }

    private async void OnDestroy()
    {
        if (_hostCoordinator is null)
        {
            return;
        }

        _hostCoordinator.StateChanged -= OnModuleStateChanged;
        await _hostCoordinator.DisposeAsync();
        _hostCoordinator = null;
    }

    public async Task<ModuleSwitchResult> SwitchToAsync(
        GameplayModuleId moduleId,
        CancellationToken cancellationToken = default)
    {
        if (_hostCoordinator is null)
        {
            var failure = ModuleSwitchResult.Failure(ModuleUxState.Error, "Module host is not initialized.");
            uxStateOverlay?.ApplyState(failure.State);
            return failure;
        }

        var result = await _hostCoordinator.SwitchAsync(moduleId, cancellationToken);
        uxStateOverlay?.ApplyState(result.State);
        return result;
    }

    public void MarkOffline(string message)
    {
        if (_hostCoordinator?.ActiveModuleId is not { } activeModule)
        {
            return;
        }

        _hostCoordinator.MarkOffline(activeModule, message);
    }

    public void MarkError(string message)
    {
        if (_hostCoordinator?.ActiveModuleId is not { } activeModule)
        {
            return;
        }

        _hostCoordinator.MarkError(activeModule, message);
    }

    private void OnModuleStateChanged(ModuleStateSnapshot snapshot)
    {
        uxStateOverlay?.ApplyState(snapshot.State);
        ModuleStateChanged?.Invoke(snapshot);
    }
}
