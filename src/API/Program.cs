using GalacticTrader.API.Telemetry;
using GalacticTrader.API.Swagger;
using GalacticTrader.API.Secrets;
using GalacticTrader.API.Contracts;
using GalacticTrader.API.Endpoints;
using GalacticTrader.API.Security;
using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Data.Repositories.Navigation;
using GalacticTrader.Services.Caching;
using GalacticTrader.Services.Admin;
using GalacticTrader.Services.Authentication;
using GalacticTrader.Services.Communication;
using GalacticTrader.Services.Combat;
using GalacticTrader.Services.Economy;
using GalacticTrader.Services.Fleet;
using GalacticTrader.Services.Auth;
using GalacticTrader.Services.Leaderboard;
using GalacticTrader.Services.Market;
using GalacticTrader.Services.Navigation;
using GalacticTrader.Services.Npc;
using GalacticTrader.Services.Reputation;
using GalacticTrader.Services.Strategic;
using GalacticTrader.Services.Realtime;
using GalacticTrader.Services.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Prometheus;
using Serilog;
using Serilog.Events;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Data;

var logServerUrl = Environment.GetEnvironmentVariable("GT_LOG_SERVER_URL");
var logServerApiKey = Environment.GetEnvironmentVariable("GT_LOG_SERVER_API_KEY");

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "logs", "api-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true);

if (!string.IsNullOrWhiteSpace(logServerUrl))
{
    loggerConfiguration = loggerConfiguration.WriteTo.Seq(
        serverUrl: logServerUrl.Trim(),
        apiKey: string.IsNullOrWhiteSpace(logServerApiKey) ? null : logServerApiKey.Trim());
}

Log.Logger = loggerConfiguration.CreateLogger();
AppDomain.CurrentDomain.ProcessExit += (_, _) => Log.CloseAndFlush();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
builder.Configuration.AddVaultSecretsIfConfigured();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Galactic Trader API",
        Version = "v1",
        Description = "Server-authoritative simulation API for navigation, trading, combat, fleet, reputation, and communication."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste the bearer token in this format: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });

    options.OperationFilter<DefaultErrorResponsesOperationFilter>();
    options.SchemaFilter<SwaggerExampleSchemaFilter>();
});
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? builder.Configuration["ConnectionStrings:Default"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<GalacticTraderDbContext>(options =>
        options.UseInMemoryDatabase("GalacticTraderDev"));
}
else
{
    builder.Services.AddDbContext<GalacticTraderDbContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
builder.Services.AddScoped<ISectorRepository, SectorRepository>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<IGraphValidationService, GraphValidationService>();
builder.Services.AddScoped<ISectorService, SectorService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<IRoutePlanningService, RoutePlanningService>();
builder.Services.AddScoped<IAutopilotService, AutopilotService>();
builder.Services.AddScoped<ICombatService, CombatService>();
builder.Services.AddScoped<IEconomyService, EconomyService>();
builder.Services.AddScoped<IMarketTransactionService, MarketTransactionService>();
builder.Services.AddScoped<INpcService, NpcService>();
builder.Services.AddScoped<IFleetService, FleetService>();
builder.Services.AddScoped<IReputationService, ReputationService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<ICommunicationService, CommunicationService>();
builder.Services.AddScoped<IStrategicSystemsService, StrategicSystemsService>();
builder.Services.AddScoped<IDashboardRealtimeSnapshotService, DashboardRealtimeSnapshotService>();
builder.Services.AddScoped<IGlobalMetricsService, GlobalMetricsService>();
builder.Services.AddScoped<IMarketIntelligenceService, MarketIntelligenceService>();
builder.Services.AddSingleton<IBalanceControlService, BalanceControlService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddOptions<KeycloakOptions>()
    .Bind(builder.Configuration.GetSection(KeycloakOptions.SectionName));
builder.Services.AddHttpClient<ITokenValidationService, KeycloakTokenValidationService>();
builder.Services.AddScoped<IEndpointAuthorizationService, EndpointAuthorizationService>();
builder.Services.AddSingleton<IVoiceService, VoiceService>();
builder.Services.AddHostedService<TelemetryGaugeRefreshService>();
builder.Services.AddHostedService<IntelligenceReportExpiryWorker>();

var app = builder.Build();
var resolvedKeycloakOptions = app.Services.GetRequiredService<IOptions<KeycloakOptions>>().Value;
LogAuthenticationMode(app.Environment, resolvedKeycloakOptions);

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GalacticTraderDbContext>();
    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
        await ValidateStrategicSchemaSmokeCheckAsync(dbContext, CancellationToken.None);
    }
    else
    {
        await dbContext.Database.EnsureCreatedAsync();
    }

    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    await EnsureBootstrapAdminPlayerAsync(dbContext, authService, builder.Configuration, CancellationToken.None);
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseSerilogRequestLogging();
app.UseWebSockets();
var channelSockets = new ConcurrentDictionary<Guid, (WebSocket Socket, ChannelType ChannelType, string ChannelKey)>();

app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    await next();
    stopwatch.Stop();

    var route = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? "unknown";
    var durationSeconds = stopwatch.Elapsed.TotalSeconds;
    var statusCode = context.Response.StatusCode.ToString();

    PrometheusMetrics.ApiRequestDuration
        .WithLabels(context.Request.Method, route, statusCode)
        .Observe(durationSeconds);

    // Captures high-level DB-bound request time for observability.
    PrometheusMetrics.DbQueryDuration.Observe(durationSeconds);
});

app.MapMetrics("/metrics");


app.MapTelemetryEndpoints();

var auth = app.MapGroup("/api/auth")
    .WithTags("Authentication");

auth.MapPost("/register", async (
    RegisterPlayerApiRequest request,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) ||
        request.Username.Trim().Length < 3)
    {
        return Results.BadRequest(new { error = "Username must be at least 3 characters long." });
    }

    if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
    {
        return Results.BadRequest(new { error = "A valid email address is required." });
    }

    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
    {
        return Results.BadRequest(new { error = "Password must be at least 8 characters long." });
    }

    if (request.Birthdate is DateOnly birthdate &&
        birthdate > DateOnly.FromDateTime(DateTime.UtcNow))
    {
        return Results.BadRequest(new { error = "Birthdate cannot be in the future." });
    }

    if (!string.IsNullOrWhiteSpace(request.Website) &&
        !Uri.TryCreate(request.Website, UriKind.Absolute, out _))
    {
        return Results.BadRequest(new { error = "Website must be a valid absolute URL." });
    }

    try
    {
        var created = await authService.RegisterAsync(
            new RegisterPlayerRequest(
                request.Username,
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.MiddleName,
                request.Nickname,
                request.Birthdate,
                request.Gender,
                request.Pronouns,
                request.PhoneNumber,
                request.Locale,
                request.TimeZone,
                request.Website),
            cancellationToken);

        await EnsureUserAccountRolesAsync(
            dbContext,
            created.PlayerId,
            created.Username,
            created.Email,
            [AuthorizationPolicies.PlayerRole],
            cancellationToken);
        await BootstrapNewPlayerAsync(dbContext, created, cancellationToken);
        return Results.Created($"/api/auth/players/{created.PlayerId}", created);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
})
    .WithOpenApi(operation =>
    {
        operation.Summary = "Register a player account";
        operation.Description = "Creates a player identity, stores credentials, and provisions starter gameplay assets.";
        return operation;
    });

auth.MapPost("/login", async (
    LoginPlayerApiRequest request,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    IOptions<KeycloakOptions> keycloakOptionsAccessor,
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Username and password are required." });
    }

    var keycloakOptions = keycloakOptionsAccessor.Value;
    var keycloakAttempt = await TryLoginAgainstKeycloakAsync(
        request.Username,
        request.Password,
        keycloakOptions,
        httpClientFactory,
        cancellationToken);

    if (keycloakAttempt.Success &&
        keycloakAttempt.Session is { } keycloakSession)
    {
        var normalizedRoles = keycloakSession.Roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!normalizedRoles.Contains(AuthorizationPolicies.PlayerRole, StringComparer.OrdinalIgnoreCase))
        {
            normalizedRoles.Add(AuthorizationPolicies.PlayerRole);
        }

        var identity = await EnsurePlayerIdentityForKeycloakLoginAsync(
            dbContext,
            keycloakSession.KeycloakUserId,
            keycloakSession.KeycloakUserIdAsGuid,
            keycloakSession.Username,
            keycloakSession.Email,
            normalizedRoles,
            cancellationToken);

        await BootstrapNewPlayerAsync(
            dbContext,
            identity,
            cancellationToken,
            keycloakSession.KeycloakUserIdAsGuid);

        return Results.Ok(new LoginResult(identity, keycloakSession.AccessToken, keycloakSession.ExpiresAtUtc));
    }

    if (keycloakAttempt.InvalidCredentials &&
        !keycloakOptions.AllowLocalFallbackOnInvalidCredentials)
    {
        return Results.Unauthorized();
    }

    var loginResult = await authService.LoginAsync(
        new LoginPlayerRequest(request.Username, request.Password),
        cancellationToken);

    return loginResult is null
        ? Results.Unauthorized()
        : Results.Ok(loginResult);
})
    .WithOpenApi(operation =>
    {
        operation.Summary = "Authenticate and get bearer token";
        operation.Description = "Validates credentials and returns a temporary bearer token suitable for API calls.";
        return operation;
    });

auth.MapGet("/validate", async (
    string token,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    var session = await authService.ValidateTokenAsync(token, cancellationToken);
    return session is null ? Results.Unauthorized() : Results.Ok(session);
})
    .WithOpenApi(operation =>
    {
        operation.Summary = "Validate bearer token";
        operation.Description = "Returns the active session when a bearer token is still valid.";
        return operation;
    });


app.MapNavigationEndpoints(RequireMapAdminAsync);
app.MapCoreGameplayEndpoints(RequireAnyRoleAsync, RequireOwnerOrAdminAsync);
app.MapStrategicEndpoints(RequireAnyRoleAsync, RequireOwnerOrAdminAsync, ResolveAuthenticatedActorAsync);

static bool IsLegacyAdminKeyEnabled(IConfiguration configuration, IHostEnvironment environment)
{
    var configured = configuration["Admin:AllowLegacyKeyAuth"]
        ?? configuration["Admin__AllowLegacyKeyAuth"];

    if (bool.TryParse(configured, out var enabled))
    {
        return enabled;
    }

    return environment.IsDevelopment();
}

static bool IsAdminAuthorizedByLegacyKey(HttpContext context, IConfiguration configuration)
{
    var expectedKey = configuration["Admin:Key"]
        ?? configuration["Admin__Key"];

    if (string.IsNullOrWhiteSpace(expectedKey))
    {
        return false;
    }

    if (!context.Request.Headers.TryGetValue("X-Admin-Key", out var providedKey))
    {
        return false;
    }

    return string.Equals(providedKey.ToString(), expectedKey, StringComparison.Ordinal);
}

static async Task<IResult?> RequireAdminBalanceAuthorizationAsync(
    HttpContext context,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    IConfiguration configuration,
    CancellationToken cancellationToken)
{
    // Prefer bearer-role auth when provided.
    if (TryReadBearerToken(context, out _))
    {
        return await RequireAnyRoleAsync(
            context,
            authService,
            dbContext,
            [AuthorizationPolicies.AdminRole],
            cancellationToken);
    }

    var hostEnvironment = context.RequestServices.GetRequiredService<IHostEnvironment>();
    var legacyKeyEnabled = IsLegacyAdminKeyEnabled(configuration, hostEnvironment);
    var legacyKeyHeaderPresent = context.Request.Headers.ContainsKey("X-Admin-Key");

    // Temporary migration path for legacy automation using X-Admin-Key.
    if (legacyKeyEnabled && IsAdminAuthorizedByLegacyKey(context, configuration))
    {
        PrometheusMetrics.AdminLegacyKeyAuthorizationAttempts.WithLabels("success").Inc();
        Log.Warning("Deprecated X-Admin-Key admin authentication path used.");
        return null;
    }

    if (legacyKeyHeaderPresent)
    {
        var failureReason = legacyKeyEnabled ? "invalid" : "disabled";
        PrometheusMetrics.AdminLegacyKeyAuthorizationAttempts.WithLabels(failureReason).Inc();
        Log.Warning(
            "Rejected deprecated X-Admin-Key admin authentication attempt. reason={Reason}",
            failureReason);
    }

    return Results.Unauthorized();
}

static bool IsKeycloakCredentialLoginConfigured(KeycloakOptions options)
{
    return !string.IsNullOrWhiteSpace(options.ServerUrl) &&
           !string.IsNullOrWhiteSpace(options.Realm) &&
           !string.IsNullOrWhiteSpace(options.ClientId);
}

static void LogAuthenticationMode(IHostEnvironment environment, KeycloakOptions options)
{
    var keycloakCredentialLoginConfigured = IsKeycloakCredentialLoginConfigured(options);
    Log.Information(
        "Authentication mode resolved. environment={Environment}; keycloakCredentialLoginConfigured={KeycloakConfigured}; allowLocalFallbackOnInvalidCredentials={AllowLocalFallback}",
        environment.EnvironmentName,
        keycloakCredentialLoginConfigured,
        options.AllowLocalFallbackOnInvalidCredentials);
}


app.MapAdminBalanceEndpoints(RequireAdminBalanceAuthorizationAsync);
app.MapCommunicationEndpoints(channelSockets, ResolveEffectivePlayerIdAsync, RequireOwnerOrAdminAsync);

app.Run();

static async Task<(bool Success, bool InvalidCredentials, (string AccessToken, DateTimeOffset ExpiresAtUtc, string KeycloakUserId, Guid KeycloakUserIdAsGuid, string Username, string Email, IReadOnlyCollection<string> Roles)? Session)> TryLoginAgainstKeycloakAsync(
    string username,
    string password,
    KeycloakOptions options,
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(options.ServerUrl) ||
        string.IsNullOrWhiteSpace(options.Realm) ||
        string.IsNullOrWhiteSpace(options.ClientId))
    {
        return (false, false, null);
    }

    var tokenEndpoint = $"{options.ServerUrl.TrimEnd('/')}/realms/{Uri.EscapeDataString(options.Realm.Trim())}/protocol/openid-connect/token";
    var payload = new Dictionary<string, string>
    {
        ["grant_type"] = "password",
        ["client_id"] = options.ClientId.Trim(),
        ["username"] = username.Trim(),
        ["password"] = password,
        ["scope"] = "openid profile email"
    };

    if (!string.IsNullOrWhiteSpace(options.ClientSecret))
    {
        payload["client_secret"] = options.ClientSecret.Trim();
    }

    using var form = new FormUrlEncodedContent(payload);
    return await ExecuteKeycloakLoginRequestAsync(
        tokenEndpoint,
        form,
        username,
        httpClientFactory,
        cancellationToken);
}

static async Task<(bool Success, bool InvalidCredentials, (string AccessToken, DateTimeOffset ExpiresAtUtc, string KeycloakUserId, Guid KeycloakUserIdAsGuid, string Username, string Email, IReadOnlyCollection<string> Roles)? Session)> ExecuteKeycloakLoginRequestAsync(
    string tokenEndpoint,
    FormUrlEncodedContent form,
    string requestedUsername,
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken)
{
    using var httpClient = httpClientFactory.CreateClient();
    HttpResponseMessage response;
    try
    {
        response = await httpClient.PostAsync(tokenEndpoint, form, cancellationToken);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        return (false, false, null);
    }
    catch (HttpRequestException)
    {
        return (false, false, null);
    }

    if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
    {
        return (false, true, null);
    }

    if (!response.IsSuccessStatusCode)
    {
        return (false, false, null);
    }

    using var payloadJson = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
    if (payloadJson is null ||
        !payloadJson.RootElement.TryGetProperty("access_token", out var accessTokenElement) ||
        accessTokenElement.ValueKind != JsonValueKind.String)
    {
        return (false, false, null);
    }

    var accessToken = accessTokenElement.GetString();
    if (string.IsNullOrWhiteSpace(accessToken))
    {
        return (false, false, null);
    }

    var expiresInSeconds =
        payloadJson.RootElement.TryGetProperty("expires_in", out var expiresElement) &&
        expiresElement.ValueKind == JsonValueKind.Number &&
        expiresElement.TryGetInt32(out var parsedSeconds)
            ? parsedSeconds
            : 0;

    JwtSecurityToken jwt;
    try
    {
        jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
    }
    catch (ArgumentException)
    {
        return (false, false, null);
    }

    var keycloakUserId = jwt.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value;
    if (string.IsNullOrWhiteSpace(keycloakUserId))
    {
        return (false, false, null);
    }

    var resolvedUsername =
        jwt.Claims.FirstOrDefault(claim => claim.Type == "preferred_username")?.Value
        ?? requestedUsername.Trim();
    var resolvedEmail =
        jwt.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value
        ?? jwt.Claims.FirstOrDefault(claim => claim.Type == "email")?.Value
        ?? $"{resolvedUsername}@local.invalid";

    var expiresAtUtc = expiresInSeconds > 0
        ? DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds)
        : (jwt.ValidTo > DateTime.MinValue ? new DateTimeOffset(jwt.ValidTo) : DateTimeOffset.UtcNow.AddHours(1));

    var roles = ExtractRolesFromToken(jwt);
    var keycloakGuid = ParseKeycloakSubjectAsGuid(keycloakUserId);

    return (true, false, (accessToken, expiresAtUtc, keycloakUserId, keycloakGuid, resolvedUsername, resolvedEmail, roles));
}

static IReadOnlyCollection<string> ExtractRolesFromToken(JwtSecurityToken token)
{
    var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var claim in token.Claims.Where(claim =>
                 claim.Type == ClaimTypes.Role ||
                 claim.Type.Equals("role", StringComparison.OrdinalIgnoreCase) ||
                 claim.Type.Equals("roles", StringComparison.OrdinalIgnoreCase)))
    {
        foreach (var role in claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            roles.Add(role);
        }
    }

    var realmAccess = token.Claims.FirstOrDefault(claim => claim.Type.Equals("realm_access", StringComparison.OrdinalIgnoreCase))?.Value;
    if (!string.IsNullOrWhiteSpace(realmAccess))
    {
        try
        {
            using var json = JsonDocument.Parse(realmAccess);
            if (json.RootElement.TryGetProperty("roles", out var roleArray) &&
                roleArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var roleElement in roleArray.EnumerateArray())
                {
                    if (roleElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var role = roleElement.GetString();
                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        roles.Add(role);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Ignore malformed realm access role payloads.
        }
    }

    return roles.ToList();
}

static Guid ParseKeycloakSubjectAsGuid(string subject)
{
    if (Guid.TryParse(subject, out var parsed))
    {
        return parsed;
    }

    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(subject));
    var guidBytes = new byte[16];
    Array.Copy(hash, guidBytes, guidBytes.Length);
    return new Guid(guidBytes);
}

static async Task<PlayerIdentity> EnsurePlayerIdentityForKeycloakLoginAsync(
    GalacticTraderDbContext dbContext,
    string keycloakUserId,
    Guid keycloakUserIdAsGuid,
    string username,
    string email,
    IReadOnlyCollection<string> requiredRoles,
    CancellationToken cancellationToken)
{
    var normalizedUsername = username.Trim();
    var normalizedEmail = email.Trim();

    var existingUserAccount = await dbContext.UserAccounts
        .AsNoTracking()
        .FirstOrDefaultAsync(
            account =>
                account.KeycloakId == keycloakUserId ||
                account.Username == normalizedUsername ||
                account.Email == normalizedEmail,
            cancellationToken);

    var existingPlayer = await dbContext.Players
        .AsNoTracking()
        .FirstOrDefaultAsync(
            player =>
                player.KeycloakUserId == keycloakUserIdAsGuid ||
                player.Username == normalizedUsername ||
                player.Email == normalizedEmail,
            cancellationToken);

    var playerId = existingPlayer?.Id ?? existingUserAccount?.Id ?? Guid.NewGuid();

    await EnsureUserAccountRolesAsync(
        dbContext,
        playerId,
        normalizedUsername,
        normalizedEmail,
        requiredRoles,
        cancellationToken,
        keycloakUserId);

    var registeredAt = existingPlayer is null
        ? DateTimeOffset.UtcNow
        : new DateTimeOffset(DateTime.SpecifyKind(existingPlayer.CreatedAt, DateTimeKind.Utc));

    return new PlayerIdentity(playerId, normalizedUsername, normalizedEmail, registeredAt);
}

static async Task<IResult?> RequireMapAdminAsync(
    HttpContext context,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken)
{
    _ = authService;
    _ = dbContext;
    return await GetEndpointAuthorizationService(context)
        .RequireMapAdminAsync(context, cancellationToken);
}

static async Task<IResult?> RequireAnyRoleAsync(
    HttpContext context,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    IReadOnlyCollection<string> allowedRoles,
    CancellationToken cancellationToken)
{
    _ = authService;
    _ = dbContext;
    return await GetEndpointAuthorizationService(context)
        .RequireAnyRoleAsync(context, allowedRoles, cancellationToken);
}

static async Task<IResult?> RequireOwnerOrAdminAsync(
    HttpContext context,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    Guid ownerPlayerId,
    CancellationToken cancellationToken)
{
    _ = authService;
    _ = dbContext;
    return await GetEndpointAuthorizationService(context)
        .RequireOwnerOrAdminAsync(context, ownerPlayerId, cancellationToken);
}

static async Task<(Guid? PlayerId, bool IsAdmin, IResult? Denied)> ResolveAuthenticatedActorAsync(
    HttpContext context,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken)
{
    _ = authService;
    _ = dbContext;
    return await GetEndpointAuthorizationService(context)
        .ResolveAuthenticatedActorAsync(context, cancellationToken);
}

static async Task<(Guid EffectivePlayerId, bool IsAdmin, IResult? Denied)> ResolveEffectivePlayerIdAsync(
    HttpContext context,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    Guid requestedPlayerId,
    CancellationToken cancellationToken)
{
    _ = authService;
    _ = dbContext;
    return await GetEndpointAuthorizationService(context)
        .ResolveEffectivePlayerIdAsync(context, requestedPlayerId, cancellationToken);
}

static bool TryReadBearerToken(HttpContext context, out string token)
{
    return GetEndpointAuthorizationService(context).TryReadBearerToken(context, out token);
}

static IEndpointAuthorizationService GetEndpointAuthorizationService(HttpContext context)
{
    return context.RequestServices.GetRequiredService<IEndpointAuthorizationService>();
}

static async Task ValidateStrategicSchemaSmokeCheckAsync(
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken)
{
    if (!dbContext.Database.IsNpgsql())
    {
        return;
    }

    var expectedStrategicTables = new[]
    {
        "SectorVolatilityCycles",
        "CorporateWars",
        "InfrastructureOwnerships",
        "TerritoryDominances",
        "InsurancePolicies",
        "InsuranceClaims",
        "IntelligenceNetworks",
        "IntelligenceReports"
    };

    var missingTables = new List<string>();
    await using var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != ConnectionState.Open;
    if (shouldClose)
    {
        await connection.OpenAsync(cancellationToken);
    }

    try
    {
        foreach (var tableName in expectedStrategicTables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public'
                      AND table_name = @tableName
                );
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var exists = result is bool present && present;
            if (!exists)
            {
                missingTables.Add(tableName);
            }
        }
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }

    if (missingTables.Count > 0)
    {
        throw new InvalidOperationException(
            $"Migration smoke check failed. Missing strategic tables: {string.Join(", ", missingTables)}");
    }
}

static async Task EnsureBootstrapAdminPlayerAsync(
    GalacticTraderDbContext dbContext,
    IAuthService authService,
    IConfiguration configuration,
    CancellationToken cancellationToken)
{
    const string bootstrapUsername = "viper";
    const string bootstrapEmail = "epiphanygs@gmail.com";

    var configuredPassword = configuration["Bootstrap:ViperPassword"]
        ?? configuration["Bootstrap__ViperPassword"];
    var bootstrapPassword = string.IsNullOrWhiteSpace(configuredPassword)
        ? "ViperDev123!"
        : configuredPassword.Trim();

    var existingPlayer = await dbContext.Players
        .AsNoTracking()
        .FirstOrDefaultAsync(
            player =>
                player.Username == bootstrapUsername ||
                player.Email == bootstrapEmail,
            cancellationToken);

    var bootstrapPlayerId = existingPlayer?.Id ?? Guid.NewGuid();
    PlayerIdentity identity;
    try
    {
        identity = await authService.RegisterAsync(
            new RegisterPlayerRequest(
                bootstrapUsername,
                bootstrapEmail,
                bootstrapPassword,
                FirstName: "Viper",
                LastName: "Pilot",
                PlayerId: bootstrapPlayerId),
            cancellationToken);
    }
    catch (InvalidOperationException)
    {
        identity = new PlayerIdentity(
            bootstrapPlayerId,
            bootstrapUsername,
            bootstrapEmail,
            DateTimeOffset.UtcNow);
    }

    await EnsureUserAccountRolesAsync(
        dbContext,
        identity.PlayerId,
        identity.Username,
        identity.Email,
        [AuthorizationPolicies.PlayerRole, AuthorizationPolicies.AdminRole, AuthorizationPolicies.MapAdminRole],
        cancellationToken);

    await BootstrapNewPlayerAsync(dbContext, identity, cancellationToken);
}

static async Task EnsureUserAccountRolesAsync(
    GalacticTraderDbContext dbContext,
    Guid playerId,
    string username,
    string email,
    IReadOnlyCollection<string> requiredRoles,
    CancellationToken cancellationToken,
    string? keycloakId = null)
{
    var normalizedUsername = username.Trim();
    var normalizedEmail = email.Trim();
    var resolvedKeycloakId = string.IsNullOrWhiteSpace(keycloakId)
        ? playerId.ToString("D")
        : keycloakId.Trim();

    var userAccount = await dbContext.UserAccounts
        .FirstOrDefaultAsync(
            account =>
                account.Id == playerId ||
                account.Username == normalizedUsername ||
                account.Email == normalizedEmail,
            cancellationToken);

    if (userAccount is null)
    {
        userAccount = new GalacticTrader.Data.Models.UserAccount
        {
            Id = playerId,
            Username = normalizedUsername,
            Email = normalizedEmail,
            FirstName = normalizedUsername,
            LastName = "Pilot",
            KeycloakId = resolvedKeycloakId,
            Roles = []
        };

        dbContext.UserAccounts.Add(userAccount);
    }
    else
    {
        userAccount.Username = normalizedUsername;
        userAccount.Email = normalizedEmail;
        userAccount.KeycloakId = resolvedKeycloakId;
    }

    foreach (var role in requiredRoles.Where(role => !string.IsNullOrWhiteSpace(role)))
    {
        if (userAccount.Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            continue;
        }

        userAccount.Roles.Add(role);
    }

    await dbContext.SaveChangesAsync(cancellationToken);
}

static async Task BootstrapNewPlayerAsync(
    GalacticTraderDbContext dbContext,
    PlayerIdentity identity,
    CancellationToken cancellationToken,
    Guid? keycloakUserId = null)
{
    const decimal starterCredits = 250_000m;
    const decimal starterShipValue = 95_000m;
    var now = DateTime.UtcNow;
    var resolvedKeycloakUserId = keycloakUserId ?? identity.PlayerId;

    var starterSector = await EnsureStarterSectorAsync(dbContext, cancellationToken);
    var player = await dbContext.Players
        .FirstOrDefaultAsync(
            existing =>
                existing.Id == identity.PlayerId ||
                existing.Username == identity.Username ||
                existing.Email == identity.Email,
            cancellationToken);

    if (player is null)
    {
        player = new Player
        {
            Id = identity.PlayerId,
            Username = identity.Username,
            Email = identity.Email,
            KeycloakUserId = resolvedKeycloakUserId,
            NetWorth = starterCredits + starterShipValue,
            LiquidCredits = starterCredits,
            ReputationScore = 0,
            AlignmentLevel = 0,
            FleetStrengthRating = 342,
            ProtectionStatus = "Protected",
            CreatedAt = now,
            LastActiveAt = now,
            IsActive = true
        };

        dbContext.Players.Add(player);
    }
    else
    {
        player.Username = identity.Username;
        player.Email = identity.Email;
        if (keycloakUserId.HasValue || player.KeycloakUserId == Guid.Empty)
        {
            player.KeycloakUserId = resolvedKeycloakUserId;
        }
        player.LastActiveAt = now;
        player.IsActive = true;
        player.ProtectionStatus = string.IsNullOrWhiteSpace(player.ProtectionStatus)
            ? "Protected"
            : player.ProtectionStatus;
    }

    if (player.LiquidCredits < starterCredits)
    {
        player.LiquidCredits = starterCredits;
    }

    var hasShip = await dbContext.Ships
        .AnyAsync(ship => ship.PlayerId == player.Id, cancellationToken);
    if (!hasShip)
    {
        var starterShip = new Ship
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Name = "Pioneer-01",
            ShipClass = "Scout",
            HullIntegrity = 180,
            MaxHullIntegrity = 180,
            ShieldCapacity = 120,
            MaxShieldCapacity = 120,
            ReactorOutput = 90,
            CargoCapacity = 160,
            CargoUsed = 0,
            SensorRange = 120,
            SignatureProfile = 35,
            CrewSlots = 6,
            Hardpoints = 2,
            HasInsurance = true,
            InsuranceRate = 0.015m,
            IsActive = true,
            IsInCombat = false,
            CurrentSectorId = starterSector.Id,
            TargetSectorId = starterSector.Id,
            StatusId = 0,
            PurchasePrice = starterShipValue,
            PurchasedAt = now,
            CurrentValue = starterShipValue
        };

        dbContext.Ships.Add(starterShip);
        hasShip = true;
    }

    var minimumNetWorth = player.LiquidCredits + (hasShip ? starterShipValue : 0m);
    if (player.NetWorth < minimumNetWorth)
    {
        player.NetWorth = minimumNetWorth;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
}

static async Task<Sector> EnsureStarterSectorAsync(
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken)
{
    var existing = await dbContext.Sectors
        .OrderByDescending(sector => sector.SecurityLevel)
        .ThenByDescending(sector => sector.EconomicIndex)
        .FirstOrDefaultAsync(cancellationToken);
    if (existing is not null)
    {
        return existing;
    }

    var sector = new Sector
    {
        Id = Guid.NewGuid(),
        Name = "New Dawn",
        X = 0,
        Y = 0,
        Z = 0,
        SecurityLevel = 90,
        HazardRating = 8,
        ResourceModifier = 1.0f,
        EconomicIndex = 85,
        SensorInterferenceLevel = 5.0f,
        ControlledByFactionId = null,
        AverageTrafficLevel = 70,
        PiratePresenceProbability = 5
    };

    dbContext.Sectors.Add(sector);
    await dbContext.SaveChangesAsync(cancellationToken);
    return sector;
}

public partial class Program;
