namespace GalacticTrader.ClientSdk.Shell;

public sealed class ModuleHotkeyRouter
{
    private readonly Dictionary<string, GameplayModuleId> _bindings;

    public ModuleHotkeyRouter(IReadOnlyList<ModuleHotkeyBinding> bindings)
    {
        _bindings = new Dictionary<string, GameplayModuleId>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.Gesture))
            {
                continue;
            }

            _bindings[binding.Gesture.Trim()] = binding.ModuleId;
        }
    }

    public IReadOnlyList<ModuleHotkeyBinding> GetBindings()
    {
        return _bindings
            .Select(static pair => new ModuleHotkeyBinding(pair.Key, pair.Value))
            .OrderBy(static binding => binding.Gesture, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool TryResolve(string gesture, out GameplayModuleId moduleId)
    {
        if (string.IsNullOrWhiteSpace(gesture))
        {
            moduleId = default;
            return false;
        }

        return _bindings.TryGetValue(gesture.Trim(), out moduleId);
    }
}
