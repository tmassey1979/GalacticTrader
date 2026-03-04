using GalacticTrader.ClientSdk.Shell;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GalacticTrader.Unity.Shell;

public abstract class UnityShellModule : MonoBehaviour, IModuleLifecycleAdapter
{
    [SerializeField] private GameplayModuleId moduleId;

    public GameplayModuleId ModuleId => moduleId;

    public virtual Task OnActivatedAsync(CancellationToken cancellationToken)
    {
        gameObject.SetActive(true);
        return Task.CompletedTask;
    }

    public virtual Task OnDeactivatedAsync(CancellationToken cancellationToken)
    {
        gameObject.SetActive(false);
        return Task.CompletedTask;
    }
}
