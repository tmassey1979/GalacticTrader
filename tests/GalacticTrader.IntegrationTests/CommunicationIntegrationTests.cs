namespace GalacticTrader.IntegrationTests;

using System.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;

public sealed class CommunicationIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CommunicationIntegrationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
    }

    [Fact]
    public async Task CommunicationMessageFlow_SubscribeSendAndList_Works()
    {
        var player = await RegisterAndLoginAsync("comms-msg");
        var channelKey = $"integration-{Guid.NewGuid():N}"[..20];

        var subscribe = await SendWithBearerTokenAsync(HttpMethod.Post, "/api/communication/subscribe", player.AccessToken, new
        {
            playerId = player.PlayerId,
            channelType = 0,
            channelKey
        });
        Assert.Equal(HttpStatusCode.OK, subscribe.StatusCode);

        var send = await SendWithBearerTokenAsync(HttpMethod.Post, "/api/communication/messages", player.AccessToken, new
        {
            playerId = player.PlayerId,
            channelType = 0,
            channelKey,
            content = "ship ping with spamlink marker"
        });
        Assert.Equal(HttpStatusCode.OK, send.StatusCode);

        var created = await send.Content.ReadFromJsonAsync<ChannelMessageResponse>();
        Assert.NotNull(created);
        Assert.True(created!.IsModerated);
        Assert.Contains("***", created.Content, StringComparison.Ordinal);

        var list = await _client.GetAsync($"/api/communication/messages/global/{channelKey}?limit=25");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var messages = await list.Content.ReadFromJsonAsync<List<ChannelMessageResponse>>();
        Assert.NotNull(messages);
        Assert.Contains(messages!, message => message.Id == created.Id);
    }

    [Fact]
    public async Task VoiceChannelFlow_CreateJoinQosLeave_Works()
    {
        var creator = await RegisterAndLoginAsync("comms-voice-a");
        var participant = await RegisterAndLoginAsync("comms-voice-b");

        var create = await SendWithBearerTokenAsync(HttpMethod.Post, "/api/communication/voice/channels", creator.AccessToken, new
        {
            creatorPlayerId = creator.PlayerId,
            mode = 1,
            scopeKey = "integration-voice"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var createdChannel = await create.Content.ReadFromJsonAsync<VoiceChannelResponse>();
        Assert.NotNull(createdChannel);

        var join = await SendWithBearerTokenAsync(HttpMethod.Post, $"/api/communication/voice/channels/{createdChannel!.ChannelId:D}/join", participant.AccessToken, new
        {
            playerId = participant.PlayerId
        });
        Assert.Equal(HttpStatusCode.OK, join.StatusCode);

        var joinedChannel = await join.Content.ReadFromJsonAsync<VoiceChannelResponse>();
        Assert.NotNull(joinedChannel);
        Assert.True(joinedChannel!.ParticipantCount >= 2);

        var qos = await _client.GetAsync($"/api/communication/voice/channels/{createdChannel.ChannelId:D}/qos");
        Assert.Equal(HttpStatusCode.OK, qos.StatusCode);

        var qosSnapshot = await qos.Content.ReadFromJsonAsync<VoiceQosResponse>();
        Assert.NotNull(qosSnapshot);
        Assert.True(qosSnapshot!.ParticipantCount >= 2);

        var leaveParticipant = await SendWithBearerTokenAsync(
            HttpMethod.Post,
            $"/api/communication/voice/channels/{createdChannel.ChannelId:D}/leave/{participant.PlayerId:D}",
            participant.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, leaveParticipant.StatusCode);

        var leaveCreator = await SendWithBearerTokenAsync(
            HttpMethod.Post,
            $"/api/communication/voice/channels/{createdChannel.ChannelId:D}/leave/{creator.PlayerId:D}",
            creator.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, leaveCreator.StatusCode);

        var qosAfterLeave = await _client.GetAsync($"/api/communication/voice/channels/{createdChannel.ChannelId:D}/qos");
        Assert.Equal(HttpStatusCode.NotFound, qosAfterLeave.StatusCode);
    }

    [Fact]
    public async Task VoiceRealtimeFlow_ActivitySignalAndSpatialMix_Works()
    {
        var creator = await RegisterAndLoginAsync("comms-real-a");
        var listener = await RegisterAndLoginAsync("comms-real-b");

        var create = await SendWithBearerTokenAsync(HttpMethod.Post, "/api/communication/voice/channels", creator.AccessToken, new
        {
            creatorPlayerId = creator.PlayerId,
            mode = 0,
            scopeKey = "integration-realtime"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var channel = await create.Content.ReadFromJsonAsync<VoiceChannelResponse>();
        Assert.NotNull(channel);

        var join = await SendWithBearerTokenAsync(HttpMethod.Post, $"/api/communication/voice/channels/{channel!.ChannelId:D}/join", listener.AccessToken, new
        {
            playerId = listener.PlayerId
        });
        Assert.Equal(HttpStatusCode.OK, join.StatusCode);

        var updateActivity = await SendWithBearerTokenAsync(HttpMethod.Post, $"/api/communication/voice/channels/{channel.ChannelId:D}/activity", creator.AccessToken, new
        {
            playerId = creator.PlayerId,
            rmsLevel = 0.61f,
            packetLossPercent = 0.2f,
            latencyMs = 31f,
            jitterMs = 3.5f
        });
        Assert.Equal(HttpStatusCode.OK, updateActivity.StatusCode);

        var activity = await updateActivity.Content.ReadFromJsonAsync<VoiceActivityResponse>();
        Assert.NotNull(activity);
        Assert.True(activity!.VoiceActivityScore > 0f);

        var publishSignal = await SendWithBearerTokenAsync(HttpMethod.Post, $"/api/communication/voice/channels/{channel.ChannelId:D}/signal", creator.AccessToken, new
        {
            senderId = creator.PlayerId,
            targetPlayerId = listener.PlayerId,
            signalType = "offer",
            payload = "sdp-offer"
        });
        Assert.Equal(HttpStatusCode.OK, publishSignal.StatusCode);

        var signal = await publishSignal.Content.ReadFromJsonAsync<VoiceSignalResponse>();
        Assert.NotNull(signal);
        Assert.Equal("offer", signal!.SignalType);

        var dequeue = await SendWithBearerTokenAsync(
            HttpMethod.Get,
            $"/api/communication/voice/channels/{channel.ChannelId:D}/signals/{listener.PlayerId:D}?limit=10",
            listener.AccessToken);
        Assert.Equal(HttpStatusCode.OK, dequeue.StatusCode);

        var signals = await dequeue.Content.ReadFromJsonAsync<List<VoiceSignalResponse>>();
        Assert.NotNull(signals);
        Assert.Contains(signals!, entry => entry.Payload == "sdp-offer" && entry.SignalType == "offer");

        var spatial = await SendWithBearerTokenAsync(HttpMethod.Post, $"/api/communication/voice/channels/{channel.ChannelId:D}/spatial-audio", listener.AccessToken, new
        {
            listenerId = listener.PlayerId,
            listenerX = 0f,
            listenerY = 0f,
            listenerZ = 0f,
            falloffDistance = 100f,
            speakers = new[]
            {
                new
                {
                    playerId = creator.PlayerId,
                    x = 20f,
                    y = 0f,
                    z = 0f,
                    baseGain = 1f
                }
            }
        });
        Assert.Equal(HttpStatusCode.OK, spatial.StatusCode);

        var mix = await spatial.Content.ReadFromJsonAsync<SpatialAudioResponse>();
        Assert.NotNull(mix);
        Assert.NotEmpty(mix!.Mix);
        Assert.Equal(creator.PlayerId, mix.Mix[0].PlayerId);
    }

    [Fact]
    public async Task CommunicationAndVoiceMutations_RequireAuthentication()
    {
        var randomPlayer = Guid.NewGuid();
        var randomChannel = Guid.NewGuid();

        var subscribe = await _client.PostAsJsonAsync("/api/communication/subscribe", new
        {
            playerId = randomPlayer,
            channelType = 0,
            channelKey = "global"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, subscribe.StatusCode);

        var send = await _client.PostAsJsonAsync("/api/communication/messages", new
        {
            playerId = randomPlayer,
            channelType = 0,
            channelKey = "global",
            content = "hello"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, send.StatusCode);

        var createChannel = await _client.PostAsJsonAsync("/api/communication/voice/channels", new
        {
            creatorPlayerId = randomPlayer,
            mode = 0,
            scopeKey = "auth-required"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, createChannel.StatusCode);

        var join = await _client.PostAsJsonAsync($"/api/communication/voice/channels/{randomChannel:D}/join", new
        {
            playerId = randomPlayer
        });
        Assert.Equal(HttpStatusCode.Unauthorized, join.StatusCode);

        var leave = await _client.PostAsync($"/api/communication/voice/channels/{randomChannel:D}/leave/{randomPlayer:D}", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, leave.StatusCode);

        var signal = await _client.PostAsJsonAsync($"/api/communication/voice/channels/{randomChannel:D}/signal", new
        {
            senderId = randomPlayer,
            signalType = "offer",
            payload = "sdp"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, signal.StatusCode);

        var activity = await _client.PostAsJsonAsync($"/api/communication/voice/channels/{randomChannel:D}/activity", new
        {
            playerId = randomPlayer,
            rmsLevel = 0.1f,
            packetLossPercent = 0f,
            latencyMs = 20f,
            jitterMs = 2f
        });
        Assert.Equal(HttpStatusCode.Unauthorized, activity.StatusCode);
    }

    [Fact]
    public async Task CommunicationAndVoiceMutations_RejectSpoofedPlayerIdentity()
    {
        var owner = await RegisterAndLoginAsync("comms-owner");
        var intruder = await RegisterAndLoginAsync("comms-intr");

        var subscribeSpoof = await SendWithBearerTokenAsync(HttpMethod.Post, "/api/communication/subscribe", intruder.AccessToken, new
        {
            playerId = owner.PlayerId,
            channelType = 0,
            channelKey = "global"
        });
        Assert.Equal(HttpStatusCode.Forbidden, subscribeSpoof.StatusCode);

        var messageSpoof = await SendWithBearerTokenAsync(HttpMethod.Post, "/api/communication/messages", intruder.AccessToken, new
        {
            playerId = owner.PlayerId,
            channelType = 0,
            channelKey = "global",
            content = "spoof"
        });
        Assert.Equal(HttpStatusCode.Forbidden, messageSpoof.StatusCode);

        var createChannel = await SendWithBearerTokenAsync(HttpMethod.Post, "/api/communication/voice/channels", owner.AccessToken, new
        {
            creatorPlayerId = owner.PlayerId,
            mode = 0,
            scopeKey = "spoof-check"
        });
        Assert.Equal(HttpStatusCode.Created, createChannel.StatusCode);
        var channel = await createChannel.Content.ReadFromJsonAsync<VoiceChannelResponse>();
        Assert.NotNull(channel);

        var joinSpoof = await SendWithBearerTokenAsync(
            HttpMethod.Post,
            $"/api/communication/voice/channels/{channel!.ChannelId:D}/join",
            intruder.AccessToken,
            new { playerId = owner.PlayerId });
        Assert.Equal(HttpStatusCode.Forbidden, joinSpoof.StatusCode);

        var leaveSpoof = await SendWithBearerTokenAsync(
            HttpMethod.Post,
            $"/api/communication/voice/channels/{channel.ChannelId:D}/leave/{owner.PlayerId:D}",
            intruder.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, leaveSpoof.StatusCode);

        var signalSpoof = await SendWithBearerTokenAsync(
            HttpMethod.Post,
            $"/api/communication/voice/channels/{channel.ChannelId:D}/signal",
            intruder.AccessToken,
            new
            {
                senderId = owner.PlayerId,
                signalType = "offer",
                payload = "spoof"
            });
        Assert.Equal(HttpStatusCode.Forbidden, signalSpoof.StatusCode);

        var activitySpoof = await SendWithBearerTokenAsync(
            HttpMethod.Post,
            $"/api/communication/voice/channels/{channel.ChannelId:D}/activity",
            intruder.AccessToken,
            new
            {
                playerId = owner.PlayerId,
                rmsLevel = 0.2f,
                packetLossPercent = 0.1f,
                latencyMs = 22f,
                jitterMs = 3f
            });
        Assert.Equal(HttpStatusCode.Forbidden, activitySpoof.StatusCode);
    }

    private async Task<(Guid PlayerId, string AccessToken)> RegisterAndLoginAsync(string usernamePrefix)
    {
        var username = $"{usernamePrefix}_{Guid.NewGuid():N}"[..20];
        var register = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email = $"{username}@gt.test",
            password = "WarpDrive123!"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password = "WarpDrive123!"
        });
        login.EnsureSuccessStatusCode();
        var payload = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
        return (payload.Player.PlayerId, payload.AccessToken);
    }

    private Task<HttpResponseMessage> SendWithBearerTokenAsync(
        HttpMethod method,
        string path,
        string accessToken,
        object? payload = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        return _client.SendAsync(request);
    }

    private sealed record LoginResponse(LoginPlayer Player, string AccessToken);
    private sealed record LoginPlayer(Guid PlayerId, string Username, string Email);

    private sealed record ChannelMessageResponse(
        Guid Id,
        Guid SenderId,
        string ChannelType,
        string ChannelKey,
        string Content,
        bool IsModerated,
        DateTime CreatedAt);

    private sealed record VoiceChannelResponse(
        Guid ChannelId,
        int Mode,
        string ScopeKey,
        string EncryptionToken,
        int ParticipantCount,
        DateTime CreatedAt);

    private sealed record VoiceQosResponse(
        Guid ChannelId,
        int ParticipantCount,
        float AverageLatencyMs,
        float AverageJitterMs,
        float AveragePacketLossPercent,
        int SpeakingParticipants,
        DateTime SampledAt);

    private sealed record VoiceActivityResponse(
        Guid ChannelId,
        Guid PlayerId,
        bool IsSpeaking,
        float VoiceActivityScore,
        DateTime UpdatedAt);

    private sealed record VoiceSignalResponse(
        Guid ChannelId,
        Guid SenderId,
        Guid? TargetPlayerId,
        string SignalType,
        string Payload,
        DateTime CreatedAt);

    private sealed record SpatialAudioResponse(
        Guid ListenerId,
        List<SpeakerMixResponse> Mix);

    private sealed record SpeakerMixResponse(
        Guid PlayerId,
        float Distance,
        float Gain,
        float Pan);
}
