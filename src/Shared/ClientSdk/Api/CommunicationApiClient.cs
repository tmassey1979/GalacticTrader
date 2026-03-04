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
        ApiClientRuntime.SetBearerToken(_httpClient, accessToken);
    }

    public async Task SubscribeAsync(SubscribeChannelApiRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/communication/subscribe", request, cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Subscribe failed", cancellationToken);
    }

    public async Task UnsubscribeAsync(SubscribeChannelApiRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/communication/unsubscribe", request, cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Unsubscribe failed", cancellationToken);
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

        await ApiClientRuntime.EnsureSuccessAsync(response, "Load channel messages failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<CommunicationChannelMessageApiDto>>(response, cancellationToken);
        return payload ?? [];
    }

    public async Task<CommunicationChannelMessageApiDto?> SendMessageAsync(
        SendChannelMessageApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/communication/messages", request, cancellationToken);

        await ApiClientRuntime.EnsureSuccessAsync(response, "Send message failed", cancellationToken);

        return await ApiClientRuntime.ReadAsync<CommunicationChannelMessageApiDto>(response, cancellationToken);
    }

    public async Task<VoiceChannelApiDto> CreateVoiceChannelAsync(
        CreateVoiceChannelApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/communication/voice/channels", request, cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Create voice channel failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<VoiceChannelApiDto>(response, cancellationToken);
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

        await ApiClientRuntime.EnsureSuccessAsync(response, "Join voice channel failed", cancellationToken);

        return await ApiClientRuntime.ReadAsync<VoiceChannelApiDto>(response, cancellationToken);
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

        await ApiClientRuntime.EnsureSuccessAsync(response, "Leave voice channel failed", cancellationToken);

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

        await ApiClientRuntime.EnsureSuccessAsync(response, "Load voice QoS failed", cancellationToken);

        return await ApiClientRuntime.ReadAsync<VoiceQosSnapshotApiDto>(response, cancellationToken);
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

        await ApiClientRuntime.EnsureSuccessAsync(response, "Update voice activity failed", cancellationToken);

        return await ApiClientRuntime.ReadAsync<VoiceActivityApiDto>(response, cancellationToken);
    }

    public async Task<VoiceSignalApiDto?> PublishVoiceSignalAsync(
        Guid channelId,
        VoiceSignalApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/communication/voice/channels/{channelId:D}/signal", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await ApiClientRuntime.EnsureSuccessAsync(response, "Publish voice signal failed", cancellationToken);

        return await ApiClientRuntime.ReadAsync<VoiceSignalApiDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<VoiceSignalApiDto>> DequeueVoiceSignalsAsync(
        Guid channelId,
        Guid playerId,
        int limit = 25,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"/api/communication/voice/channels/{channelId:D}/signals/{playerId:D}?limit={Math.Clamp(limit, 1, 200)}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }

        await ApiClientRuntime.EnsureSuccessAsync(response, "Poll voice signals failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<VoiceSignalApiDto>>(response, cancellationToken);
        return payload ?? [];
    }

    public async Task<SpatialAudioResultApiDto?> CalculateSpatialAudioAsync(
        Guid channelId,
        SpatialAudioApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/communication/voice/channels/{channelId:D}/spatial-audio", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await ApiClientRuntime.EnsureSuccessAsync(response, "Calculate spatial audio failed", cancellationToken);

        return await ApiClientRuntime.ReadAsync<SpatialAudioResultApiDto>(response, cancellationToken);
    }
}



