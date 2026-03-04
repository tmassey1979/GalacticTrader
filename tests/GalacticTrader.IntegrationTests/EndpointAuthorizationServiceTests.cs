using System.Security.Claims;
using GalacticTrader.API.Security;
using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Services.Auth;
using GalacticTrader.Services.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GalacticTrader.IntegrationTests;

public class EndpointAuthorizationServiceTests
{
    [Fact]
    public async Task RequireAnyRoleAsync_ReturnsUnauthorized_WhenBearerTokenMissing()
    {
        await using var dbContext = CreateDbContext();
        var service = new EndpointAuthorizationService(
            new FakeAuthService(_ => null),
            new FakeTokenValidationService(),
            dbContext);

        var context = new DefaultHttpContext();
        var denied = await service.RequireAnyRoleAsync(
            context,
            [AuthorizationPolicies.AdminRole],
            CancellationToken.None);

        Assert.NotNull(denied);
        Assert.Equal(StatusCodes.Status401Unauthorized, await ExecuteStatusCodeAsync(denied!));
    }

    [Fact]
    public async Task RequireAnyRoleAsync_ReturnsForbidden_WhenSessionUserLacksRole()
    {
        var playerId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.UserAccounts.Add(new GalacticTrader.Data.Models.UserAccount
        {
            Id = playerId,
            Username = "pilot",
            Email = "pilot@example.com",
            FirstName = "Pilot",
            LastName = "One",
            KeycloakId = playerId.ToString("D"),
            Roles = [AuthorizationPolicies.PlayerRole]
        });
        await dbContext.SaveChangesAsync();

        var session = BuildSession(playerId, "pilot", "pilot@example.com", "session-token");
        var service = new EndpointAuthorizationService(
            new FakeAuthService(token => token == session.AccessToken ? session : null),
            new FakeTokenValidationService(),
            dbContext);

        var context = BuildHttpContextWithBearer(session.AccessToken);
        var denied = await service.RequireAnyRoleAsync(
            context,
            [AuthorizationPolicies.AdminRole],
            CancellationToken.None);

        Assert.NotNull(denied);
        Assert.Equal(StatusCodes.Status403Forbidden, await ExecuteStatusCodeAsync(denied!));
    }

    [Fact]
    public async Task RequireOwnerOrAdminAsync_AllowsOwnerPlayerSession()
    {
        var playerId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.UserAccounts.Add(new GalacticTrader.Data.Models.UserAccount
        {
            Id = playerId,
            Username = "owner",
            Email = "owner@example.com",
            FirstName = "Owner",
            LastName = "Pilot",
            KeycloakId = playerId.ToString("D"),
            Roles = [AuthorizationPolicies.PlayerRole]
        });
        await dbContext.SaveChangesAsync();

        var session = BuildSession(playerId, "owner", "owner@example.com", "owner-token");
        var service = new EndpointAuthorizationService(
            new FakeAuthService(token => token == session.AccessToken ? session : null),
            new FakeTokenValidationService(),
            dbContext);

        var context = BuildHttpContextWithBearer(session.AccessToken);
        var denied = await service.RequireOwnerOrAdminAsync(context, playerId, CancellationToken.None);

        Assert.Null(denied);
    }

    [Fact]
    public async Task ResolveEffectivePlayerIdAsync_DeniesSpoofedPlayer_ForNonAdmin()
    {
        var callerId = Guid.NewGuid();
        var spoofedId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        dbContext.UserAccounts.Add(new GalacticTrader.Data.Models.UserAccount
        {
            Id = callerId,
            Username = "caller",
            Email = "caller@example.com",
            FirstName = "Caller",
            LastName = "Pilot",
            KeycloakId = callerId.ToString("D"),
            Roles = [AuthorizationPolicies.PlayerRole]
        });
        await dbContext.SaveChangesAsync();

        var session = BuildSession(callerId, "caller", "caller@example.com", "caller-token");
        var service = new EndpointAuthorizationService(
            new FakeAuthService(token => token == session.AccessToken ? session : null),
            new FakeTokenValidationService(),
            dbContext);

        var context = BuildHttpContextWithBearer(session.AccessToken);
        var (_, isAdmin, denied) = await service.ResolveEffectivePlayerIdAsync(
            context,
            spoofedId,
            CancellationToken.None);

        Assert.False(isAdmin);
        Assert.NotNull(denied);
        Assert.Equal(StatusCodes.Status403Forbidden, await ExecuteStatusCodeAsync(denied!));
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("D"))
            .Options;

        return new GalacticTraderDbContext(options);
    }

    private static PlayerSession BuildSession(Guid playerId, string username, string email, string accessToken)
    {
        var identity = new PlayerIdentity(playerId, username, email, DateTimeOffset.UtcNow);
        return new PlayerSession(identity, accessToken, DateTimeOffset.UtcNow.AddHours(1));
    }

    private static DefaultHttpContext BuildHttpContextWithBearer(string token)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {token}";
        return context;
    }

    private static async Task<int> ExecuteStatusCodeAsync(IResult result)
    {
        var context = new DefaultHttpContext();
        await result.ExecuteAsync(context);
        return context.Response.StatusCode;
    }

    private sealed class FakeAuthService : IAuthService
    {
        private readonly Func<string, PlayerSession?> _resolveSession;

        public FakeAuthService(Func<string, PlayerSession?> resolveSession)
        {
            _resolveSession = resolveSession;
        }

        public Task<PlayerIdentity> RegisterAsync(RegisterPlayerRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<LoginResult?> LoginAsync(LoginPlayerRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<PlayerSession?> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_resolveSession(accessToken));
        }
    }

    private sealed class FakeTokenValidationService : ITokenValidationService
    {
        public Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
        {
            return Task.FromResult<ClaimsPrincipal?>(null);
        }

        public Guid? GetUserId(ClaimsPrincipal principal)
        {
            return null;
        }

        public IEnumerable<string> GetRoles(ClaimsPrincipal principal)
        {
            return [];
        }

        public bool HasRole(ClaimsPrincipal principal, string role)
        {
            return false;
        }

        public Dictionary<string, string> GetAllClaims(ClaimsPrincipal principal)
        {
            return [];
        }
    }
}
