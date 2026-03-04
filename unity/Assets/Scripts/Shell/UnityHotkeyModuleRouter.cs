using GalacticTrader.ClientSdk.Shell;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GalacticTrader.Unity.Shell;

public sealed class UnityHotkeyModuleRouter : MonoBehaviour
{
    [SerializeField] private UnityModuleHostController? moduleHost;

    private ModuleHotkeyRouter? _router;
    private readonly Dictionary<string, KeyCode> _keyCodeCache = new(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        _router = new ModuleHotkeyRouter(
        [
            new ModuleHotkeyBinding("F1", GameplayModuleId.Dashboard),
            new ModuleHotkeyBinding("F2", GameplayModuleId.Trading),
            new ModuleHotkeyBinding("F3", GameplayModuleId.Routes),
            new ModuleHotkeyBinding("F4", GameplayModuleId.Fleet),
            new ModuleHotkeyBinding("F5", GameplayModuleId.Battles),
            new ModuleHotkeyBinding("F6", GameplayModuleId.Intel),
            new ModuleHotkeyBinding("F7", GameplayModuleId.Reputation),
            new ModuleHotkeyBinding("F8", GameplayModuleId.Territory),
            new ModuleHotkeyBinding("F9", GameplayModuleId.Communication),
            new ModuleHotkeyBinding("F10", GameplayModuleId.Settings)
        ]);
    }

    private async void Update()
    {
        if (_router is null || moduleHost is null)
        {
            return;
        }

        foreach (var binding in _router.GetBindings())
        {
            if (!TryGetKeyCode(binding.Gesture, out var keyCode))
            {
                continue;
            }

            if (!Input.GetKeyDown(keyCode))
            {
                continue;
            }

            await moduleHost.SwitchToAsync(binding.ModuleId);
            return;
        }
    }

    private bool TryGetKeyCode(string gesture, out KeyCode keyCode)
    {
        if (_keyCodeCache.TryGetValue(gesture, out keyCode))
        {
            return true;
        }

        if (Enum.TryParse<KeyCode>(gesture, ignoreCase: true, out keyCode))
        {
            _keyCodeCache[gesture] = keyCode;
            return true;
        }

        return false;
    }
}
