using GalacticTrader.Services.Communication;

namespace GalacticTrader.Tests;

public sealed class VoiceServiceTests
{
    [Fact]
    public async Task CreateJoinAndSignal_RoutesToReceiverQueue()
    {
        var service = new VoiceService();
        var creatorId = Guid.NewGuid();
        var peerId = Guid.NewGuid();

        var channel = await service.CreateChannelAsync(new CreateVoiceChannelRequest
        {
            CreatorPlayerId = creatorId,
            Mode = VoiceMode.EncryptedPrivate,
            ScopeKey = "private-alpha"
        });

        await service.JoinChannelAsync(channel.ChannelId, new JoinVoiceChannelRequest { PlayerId = peerId });

        await service.PublishSignalAsync(channel.ChannelId, new VoiceSignalRequest
        {
            SenderId = creatorId,
            TargetPlayerId = peerId,
            SignalType = "offer",
            Payload = "sdp-offer"
        });

        var creatorSignals = await service.DequeueSignalsAsync(channel.ChannelId, creatorId);
        var peerSignals = await service.DequeueSignalsAsync(channel.ChannelId, peerId);

        Assert.Empty(creatorSignals);
        Assert.Single(peerSignals);
        Assert.Equal("offer", peerSignals[0].SignalType);
    }

    [Fact]
    public async Task UpdateActivityAndQos_ReturnsVoiceAndNetworkStats()
    {
        var service = new VoiceService();
        var playerId = Guid.NewGuid();
        var channel = await service.CreateChannelAsync(new CreateVoiceChannelRequest
        {
            CreatorPlayerId = playerId,
            Mode = VoiceMode.Fleet,
            ScopeKey = "fleet-7"
        });

        var activity = await service.UpdateActivityAsync(channel.ChannelId, new VoiceActivityRequest
        {
            PlayerId = playerId,
            RmsLevel = 0.08f,
            PacketLossPercent = 2.5f,
            LatencyMs = 85f,
            JitterMs = 12f
        });

        var qos = await service.GetQosSnapshotAsync(channel.ChannelId);

        Assert.NotNull(activity);
        Assert.True(activity!.IsSpeaking);
        Assert.NotNull(qos);
        Assert.True(qos!.AverageLatencyMs >= 80f);
        Assert.Equal(1, qos.ParticipantCount);
    }

    [Fact]
    public async Task SpatialMix_AttenuatesByDistance()
    {
        var service = new VoiceService();
        var listener = Guid.NewGuid();
        var nearSpeaker = Guid.NewGuid();
        var farSpeaker = Guid.NewGuid();

        var channel = await service.CreateChannelAsync(new CreateVoiceChannelRequest
        {
            CreatorPlayerId = listener,
            Mode = VoiceMode.Proximity,
            ScopeKey = "sector-1"
        });

        await service.JoinChannelAsync(channel.ChannelId, new JoinVoiceChannelRequest { PlayerId = nearSpeaker });
        await service.JoinChannelAsync(channel.ChannelId, new JoinVoiceChannelRequest { PlayerId = farSpeaker });

        var mix = await service.CalculateSpatialMixAsync(channel.ChannelId, new SpatialAudioRequest
        {
            ListenerId = listener,
            ListenerX = 0,
            ListenerY = 0,
            ListenerZ = 0,
            FalloffDistance = 100,
            Speakers =
            [
                new SpeakerSample { PlayerId = nearSpeaker, X = 10, Y = 0, Z = 0, BaseGain = 1f },
                new SpeakerSample { PlayerId = farSpeaker, X = 200, Y = 0, Z = 0, BaseGain = 1f }
            ]
        });

        Assert.NotNull(mix);
        var nearGain = mix!.Mix.Single(item => item.PlayerId == nearSpeaker).Gain;
        var farGain = mix.Mix.Single(item => item.PlayerId == farSpeaker).Gain;
        Assert.True(nearGain > farGain);
    }
}
