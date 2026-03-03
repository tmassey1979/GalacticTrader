using System.Security.Claims;
using GalacticTrader.Services.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GalacticTrader.Tests;

public class KeycloakTokenValidationServiceTests
{
    [Fact]
    public void GetRoles_ExtractsRealmAndResourceRoles()
    {
        var service = CreateService();
        var principal = BuildPrincipal(
            new Claim("realm_access", "{\"roles\":[\"admin\",\"map_admin\"]}"),
            new Claim("resource_access", "{\"map-generator-desktop\":{\"roles\":[\"player\"]}}"));

        var roles = service.GetRoles(principal).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("admin", roles);
        Assert.Contains("map_admin", roles);
        Assert.Contains("player", roles);
    }

    [Fact]
    public void HasRole_ReturnsTrueForRoleFromRealmAccess()
    {
        var service = CreateService();
        var principal = BuildPrincipal(new Claim("realm_access", "{\"roles\":[\"map_admin\"]}"));

        Assert.True(service.HasRole(principal, "map_admin"));
        Assert.False(service.HasRole(principal, "moderator"));
    }

    private static KeycloakTokenValidationService CreateService()
    {
        var options = Options.Create(new KeycloakOptions());
        return new KeycloakTokenValidationService(options, NullLogger<KeycloakTokenValidationService>.Instance, new HttpClient());
    }

    private static ClaimsPrincipal BuildPrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
