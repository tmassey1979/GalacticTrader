using GalacticTrader.ClientSdk.Shell;
using UnityEngine;

namespace GalacticTrader.Unity.Shell;

public sealed class ModuleUxStateOverlayController : MonoBehaviour
{
    [SerializeField] private GameObject? loadingView;
    [SerializeField] private GameObject? offlineView;
    [SerializeField] private GameObject? errorView;

    public void ApplyState(ModuleUxState state)
    {
        SetVisible(loadingView, state == ModuleUxState.Loading);
        SetVisible(offlineView, state == ModuleUxState.Offline);
        SetVisible(errorView, state == ModuleUxState.Error);
    }

    private static void SetVisible(GameObject? view, bool visible)
    {
        if (view is null)
        {
            return;
        }

        if (view.activeSelf != visible)
        {
            view.SetActive(visible);
        }
    }
}
