namespace GalacticTrader.ClientSdk.Shell;

public interface IModuleLifecycleAdapter
{
    GameplayModuleId ModuleId { get; }

    Task OnActivatedAsync(CancellationToken cancellationToken);

    Task OnDeactivatedAsync(CancellationToken cancellationToken);
}
