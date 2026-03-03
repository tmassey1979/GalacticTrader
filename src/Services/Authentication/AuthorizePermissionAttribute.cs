namespace GalacticTrader.Services.Authentication;

/// <summary>
/// Attribute for requiring specific permissions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizePermissionAttribute : Attribute
{
    public string Permission { get; }

    public AuthorizePermissionAttribute(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}
