namespace GalacticTrader.Services.Authentication;

/// <summary>
/// Attribute for requiring specific roles.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeRoleAttribute : Attribute
{
    public string[] Roles { get; }

    public AuthorizeRoleAttribute(params string[] roles)
    {
        Roles = roles ?? Array.Empty<string>();
    }
}
