namespace GalacticTrader.Desktop.Navigation;

public static class ModuleBreadcrumbBuilder
{
    public static string Build(string? moduleName)
    {
        var normalized = string.IsNullOrWhiteSpace(moduleName)
            ? "Dashboard"
            : moduleName.Trim();

        return $"Command / {normalized}";
    }
}
