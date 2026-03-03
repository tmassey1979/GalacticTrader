using System.Net.Http;
using System.Net.Http.Json;

namespace GalacticTrader.Desktop.Api;

public sealed class CommunicationApiClient
{
    private readonly HttpClient _httpClient;

    public CommunicationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetBearerToken(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task SubscribeAsync(SubscribeChannelApiRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/communication/subscribe", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Subscribe failed ({(int)response.StatusCode}): {detail}");
        }
    }

    public async Task<IReadOnlyList<CommunicationChannelMessageApiDto>> GetRecentMessagesAsync(
        string channelType,
        string channelKey,
        int limit = 75,
        CancellationToken cancellationToken = default)
    {
        var escapedType = Uri.EscapeDataString(channelType.Trim().ToLowerInvariant());
        var escapedKey = Uri.EscapeDataString(channelKey);
        var response = await _httpClient.GetAsync(
            $"/api/communication/messages/{escapedType}/{escapedKey}?limit={Math.Clamp(limit, 1, 200)}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load channel messages failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<List<CommunicationChannelMessageApiDto>>(cancellationToken);
        return payload ?? [];
    }

    public async Task<CommunicationChannelMessageApiDto?> SendMessageAsync(
        SendChannelMessageApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/communication/messages", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Send message failed ({(int)response.StatusCode}): {detail}");
        }

        return await response.Content.ReadFromJsonAsync<CommunicationChannelMessageApiDto>(cancellationToken);
    }

    public async Task<VoiceChannelApiDto> CreateVoiceChannelAsync(
        CreateVoiceChannelApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/communication/voice/channels", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Create voice channel failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<VoiceChannelApiDto>(cancellationToken);
        return payload ?? throw new InvalidOperationException("Voice channel create response was empty.");
    }

    public async Task<VoiceChannelApiDto?> JoinVoiceChannelAsync(
        Guid channelId,
        JoinVoiceChannelApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/communication/voice/channels/{channelId:D}/join", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Join voice channel failed ({(int)response.StatusCode}): {detail}");
        }

        return await response.Content.ReadFromJsonAsync<VoiceChannelApiDto>(cancellationToken);
    }

    public async Task<bool> LeaveVoiceChannelAsync(
        Guid channelId,
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"/api/communication/voice/channels/{channelId:D}/leave/{playerId:D}", content: null, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Leave voice channel failed ({(int)response.StatusCode}): {detail}");
        }

        return true;
    }

    public async Task<VoiceQosSnapshotApiDto?> GetVoiceQosSnapshotAsync(
        Guid channelId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/communication/voice/channels/{channelId:D}/qos", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load voice QoS failed ({(int)response.StatusCode}): {detail}");
        }

        return await response.Content.ReadFromJsonAsync<VoiceQosSnapshotApiDto>(cancellationToken);
    }

    public async Task<VoiceActivityApiDto?> UpdateVoiceActivityAsync(
        Guid channelId,
        VoiceActivityApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/communication/voice/channels/{channelId:D}/activity", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Update voice activity failed ({(int)response.StatusCode}): {detail}");
        }

        return await response.Content.ReadFromJsonAsync<VoiceActivityApiDto>(cancellationToken);
    }
}
