using GalacticTrader.Data;
using GalacticTrader.Data.Repositories.Navigation;
using GalacticTrader.Services.Caching;
using GalacticTrader.Services.Navigation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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

builder.Services.AddScoped<ICacheService, InMemoryCacheService>();
builder.Services.AddScoped<ISectorRepository, SectorRepository>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<IGraphValidationService, GraphValidationService>();
builder.Services.AddScoped<ISectorService, SectorService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<IRoutePlanningService, RoutePlanningService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var sectors = app.MapGroup("/api/navigation/sectors")
    .WithTags("Navigation - Sectors");

sectors.MapGet("/", async (ISectorService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetAllSectorsAsync(cancellationToken);
    return Results.Ok(result);
});

sectors.MapGet("/{sectorId:guid}", async (Guid sectorId, ISectorService service, CancellationToken cancellationToken) =>
{
    var sector = await service.GetSectorByIdAsync(sectorId, cancellationToken);
    return sector is null ? Results.NotFound() : Results.Ok(sector);
});

sectors.MapGet("/{sectorId:guid}/adjacent", async (Guid sectorId, ISectorService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetAdjacentSectorsAsync(sectorId, cancellationToken);
    return Results.Ok(result);
});

sectors.MapGet("/high-security", async (int? threshold, ISectorService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetHighSecuritySectorsAsync(threshold ?? 70, cancellationToken);
    return Results.Ok(result);
});

sectors.MapGet("/high-risk", async (int? threshold, ISectorService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetHighRiskSectorsAsync(threshold ?? 70, cancellationToken);
    return Results.Ok(result);
});

sectors.MapPost("/", async (CreateSectorRequest request, ISectorService service, CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateSectorAsync(request.Name, request.X, request.Y, request.Z, cancellationToken);
        return Results.Created($"/api/navigation/sectors/{created.Id}", created);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

sectors.MapPut("/{sectorId:guid}", async (Guid sectorId, UpdateSectorRequest request, ISectorService service, CancellationToken cancellationToken) =>
{
    var updated = await service.UpdateSectorAsync(
        sectorId,
        request.SecurityLevel,
        request.HazardRating,
        request.FactionId,
        cancellationToken);

    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

sectors.MapDelete("/{sectorId:guid}", async (Guid sectorId, ISectorService service, CancellationToken cancellationToken) =>
{
    var deleted = await service.DeleteSectorAsync(sectorId, cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
});

var routes = app.MapGroup("/api/navigation/routes")
    .WithTags("Navigation - Routes");

routes.MapGet("/", async (IRouteService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetAllRoutesAsync(cancellationToken);
    return Results.Ok(result);
});

routes.MapGet("/{routeId:guid}", async (Guid routeId, IRouteService service, CancellationToken cancellationToken) =>
{
    var route = await service.GetRouteByIdAsync(routeId, cancellationToken);
    return route is null ? Results.NotFound() : Results.Ok(route);
});

routes.MapGet("/outbound/{sectorId:guid}", async (Guid sectorId, IRouteService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetOutboundRoutesAsync(sectorId, cancellationToken);
    return Results.Ok(result);
});

routes.MapGet("/inbound/{sectorId:guid}", async (Guid sectorId, IRouteService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetInboundRoutesAsync(sectorId, cancellationToken);
    return Results.Ok(result);
});

routes.MapGet("/between/{sectorAId:guid}/{sectorBId:guid}", async (
    Guid sectorAId,
    Guid sectorBId,
    IRouteService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GetRoutesBetweenAsync(sectorAId, sectorBId, cancellationToken);
    return Results.Ok(result);
});

routes.MapGet("/dangerous", async (int? riskThreshold, IRouteService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetDangerousRoutesAsync(riskThreshold ?? 70, cancellationToken);
    return Results.Ok(result);
});

routes.MapGet("/legal", async (IRouteService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetLegalRoutesAsync(cancellationToken);
    return Results.Ok(result);
});

routes.MapPost("/", async (CreateRouteRequest request, IRouteService service, CancellationToken cancellationToken) =>
{
    try
    {
        var created = await service.CreateRouteAsync(
            request.FromSectorId,
            request.ToSectorId,
            request.LegalStatus,
            request.WarpGateType,
            cancellationToken);
        return Results.Created($"/api/navigation/routes/{created.Id}", created);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

routes.MapPut("/{routeId:guid}", async (Guid routeId, UpdateRouteRequest request, IRouteService service, CancellationToken cancellationToken) =>
{
    var updated = await service.UpdateRouteAsync(routeId, request.LegalStatus, request.BaseRiskScore, cancellationToken);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

routes.MapDelete("/{routeId:guid}", async (Guid routeId, IRouteService service, CancellationToken cancellationToken) =>
{
    var deleted = await service.DeleteRouteAsync(routeId, cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/api/navigation/graph/validate", async (IGraphValidationService validationService, CancellationToken cancellationToken) =>
{
    var report = await validationService.ValidateGraphAsync(cancellationToken);
    return Results.Ok(report);
})
    .WithTags("Navigation - Graph");

var planning = app.MapGroup("/api/navigation/planning")
    .WithTags("Navigation - Planning");

planning.MapGet("/{fromSectorId:guid}/{toSectorId:guid}", async (
    Guid fromSectorId,
    Guid toSectorId,
    TravelMode? mode,
    string? algorithm,
    IRoutePlanningService planningService,
    CancellationToken cancellationToken) =>
{
    var plan = await planningService.CalculateRouteAsync(
        fromSectorId,
        toSectorId,
        mode ?? TravelMode.Standard,
        string.IsNullOrWhiteSpace(algorithm) ? "dijkstra" : algorithm,
        cancellationToken);

    return plan is null
        ? Results.NotFound(new { error = "No route found between the selected sectors." })
        : Results.Ok(plan);
});

planning.MapGet("/{fromSectorId:guid}/{toSectorId:guid}/optimize", async (
    Guid fromSectorId,
    Guid toSectorId,
    IRoutePlanningService planningService,
    CancellationToken cancellationToken) =>
{
    var optimization = await planningService.GetOptimizedRoutesAsync(fromSectorId, toSectorId, cancellationToken);
    return Results.Ok(optimization);
});

app.Run();

public sealed record CreateSectorRequest(string Name, float X, float Y, float Z);
public sealed record UpdateSectorRequest(int? SecurityLevel, int? HazardRating, Guid? FactionId);
public sealed record CreateRouteRequest(Guid FromSectorId, Guid ToSectorId, string LegalStatus, string WarpGateType);
public sealed record UpdateRouteRequest(string? LegalStatus, float? BaseRiskScore);
