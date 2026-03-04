using System.Net.Http;
using System.Net.Http.Json;
using System.Net;

namespace GalacticTrader.Desktop.Api;

public sealed class FleetApiClient
{
    private readonly HttpClient _httpClient;

    public FleetApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetBearerToken(string accessToken)
    {
        ApiClientRuntime.SetBearerToken(_httpClient, accessToken);
    }

    public async Task<IReadOnlyList<ShipApiDto>> GetPlayerShipsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/fleet/players/{playerId}/ships", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load ships failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<ShipApiDto>>(response, cancellationToken);
        return payload ?? [];
    }

    public async Task<EscortSummaryApiDto?> GetEscortSummaryAsync(
        Guid playerId,
        string formation = "Defensive",
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/fleet/players/{playerId}/escort?formation={formation}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await ApiClientRuntime.EnsureSuccessAsync(response, "Load escort summary failed", cancellationToken);

        return await ApiClientRuntime.ReadAsync<EscortSummaryApiDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<ShipTemplateApiDto>> GetShipTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/fleet/templates", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load ship templates failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<ShipTemplateApiDto>>(response, cancellationToken);
        return payload ?? [];
    }

    public async Task<ShipApiDto> PurchaseShipAsync(PurchaseShipApiRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/fleet/ships/purchase", request, cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Purchase ship failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<ShipApiDto>(response, cancellationToken);
        return payload ?? throw new InvalidOperationException("Purchase ship response was empty.");
    }

    public async Task<ConvoySimulationResultApiDto?> SimulateConvoyAsync(
        ConvoySimulationApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/fleet/convoy/simulate", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ApiClientRuntime.EnsureSuccessAsync(response, "Convoy simulation failed", cancellationToken);
        return await ApiClientRuntime.ReadAsync<ConvoySimulationResultApiDto>(response, cancellationToken);
    }
}



