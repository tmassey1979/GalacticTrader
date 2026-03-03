namespace GalacticTrader.IntegrationTests;

using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

public sealed class CommunicationIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CommunicationIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
    }

    [Fact]
    public async Task CommunicationMessageFlow_SubscribeSendAndList_Works()
    {
        var playerId = await SeedPlayerAsync("comms-msg");
        var channelKey = $"integration-{Guid.NewGuid():N}"[..20];

        var subscribe = await _client.PostAsJsonAsync("/api/communication/subscribe", new
        {
            playerId,
            channelType = 0,
            channelKey
        });
        Assert.Equal(HttpStatusCode.OK, subscribe.StatusCode);

        var send = await _client.PostAsJsonAsync("/api/communication/messages", new
        {
            playerId,
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
        var creatorId = await SeedPlayerAsync("comms-voice-a");
        var participantId = await SeedPlayerAsync("comms-voice-b");

        var create = await _client.PostAsJsonAsync("/api/communication/voice/channels", new
        {
            creatorPlayerId = creatorId,
            mode = 1,
            scopeKey = "integration-voice"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var createdChannel = await create.Content.ReadFromJsonAsync<VoiceChannelResponse>();
        Assert.NotNull(createdChannel);

        var join = await _client.PostAsJsonAsync($"/api/communication/voice/channels/{createdChannel!.ChannelId:D}/join", new
        {
            playerId = participantId
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

        var leaveParticipant = await _client.PostAsync($"/api/communication/voice/channels/{createdChannel.ChannelId:D}/leave/{participantId:D}", content: null);
        Assert.Equal(HttpStatusCode.NoContent, leaveParticipant.StatusCode);

        var leaveCreator = await _client.PostAsync($"/api/communication/voice/channels/{createdChannel.ChannelId:D}/leave/{creatorId:D}", content: null);
        Assert.Equal(HttpStatusCode.NoContent, leaveCreator.StatusCode);

        var qosAfterLeave = await _client.GetAsync($"/api/communication/voice/channels/{createdChannel.ChannelId:D}/qos");
        Assert.Equal(HttpStatusCode.NotFound, qosAfterLeave.StatusCode);
    }

    [Fact]
    public async Task VoiceRealtimeFlow_ActivitySignalAndSpatialMix_Works()
    {
        var creatorId = await SeedPlayerAsync("comms-real-a");
        var listenerId = await SeedPlayerAsync("comms-real-b");

        var create = await _client.PostAsJsonAsync("/api/communication/voice/channels", new
        {
            creatorPlayerId = creatorId,
            mode = 0,
            scopeKey = "integration-realtime"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var channel = await create.Content.ReadFromJsonAsync<VoiceChannelResponse>();
        Assert.NotNull(channel);

        var join = await _client.PostAsJsonAsync($"/api/communication/voice/channels/{channel!.ChannelId:D}/join", new
        {
            playerId = listenerId
        });
        Assert.Equal(HttpStatusCode.OK, join.StatusCode);

        var updateActivity = await _client.PostAsJsonAsync($"/api/communication/voice/channels/{channel.ChannelId:D}/activity", new
        {
            playerId = creatorId,
            rmsLevel = 0.61f,
            packetLossPercent = 0.2f,
            latencyMs = 31f,
            jitterMs = 3.5f
        });
        Assert.Equal(HttpStatusCode.OK, updateActivity.StatusCode);

        var activity = await updateActivity.Content.ReadFromJsonAsync<VoiceActivityResponse>();
        Assert.NotNull(activity);
        Assert.True(activity!.VoiceActivityScore > 0f);

        var publishSignal = await _client.PostAsJsonAsync($"/api/communication/voice/channels/{channel.ChannelId:D}/signal", new
        {
            senderId = creatorId,
            targetPlayerId = listenerId,
            signalType = "offer",
            payload = "sdp-offer"
        });
        Assert.Equal(HttpStatusCode.OK, publishSignal.StatusCode);

        var signal = await publishSignal.Content.ReadFromJsonAsync<VoiceSignalResponse>();
        Assert.NotNull(signal);
        Assert.Equal("offer", signal!.SignalType);

        var dequeue = await _client.GetAsync($"/api/communication/voice/channels/{channel.ChannelId:D}/signals/{listenerId:D}?limit=10");
        Assert.Equal(HttpStatusCode.OK, dequeue.StatusCode);

        var signals = await dequeue.Content.ReadFromJsonAsync<List<VoiceSignalResponse>>();
        Assert.NotNull(signals);
        Assert.Contains(signals!, entry => entry.Payload == "sdp-offer" && entry.SignalType == "offer");

        var spatial = await _client.PostAsJsonAsync($"/api/communication/voice/channels/{channel.ChannelId:D}/spatial-audio", new
        {
            listenerId,
            listenerX = 0f,
            listenerY = 0f,
            listenerZ = 0f,
            falloffDistance = 100f,
            speakers = new[]
            {
                new
                {
                    playerId = creatorId,
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
        Assert.Equal(creatorId, mix.Mix[0].PlayerId);
    }

    private async Task<Guid> SeedPlayerAsync(string usernamePrefix)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GalacticTraderDbContext>();

        var now = DateTime.UtcNow;
        var playerId = Guid.NewGuid();
        dbContext.Players.Add(new Player
        {
            Id = playerId,
            Username = $"{usernamePrefix}-{playerId:N}"[..24],
            Email = $"{usernamePrefix}-{playerId:N}@gt.test",
            KeycloakUserId = Guid.NewGuid(),
            NetWorth = 100000m,
            LiquidCredits = 25000m,
            ReputationScore = 50,
            AlignmentLevel = 10,
            FleetStrengthRating = 120,
            ProtectionStatus = "Guarded",
            CreatedAt = now,
            LastActiveAt = now,
            IsActive = true
        });

        await dbContext.SaveChangesAsync();
        return playerId;
    }

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
