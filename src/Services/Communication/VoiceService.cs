namespace GalacticTrader.Services.Communication;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

public sealed class VoiceService : IVoiceService
{
    private readonly ConcurrentDictionary<Guid, VoiceChannelState> _channels = new();

    public Task<VoiceChannelDto> CreateChannelAsync(CreateVoiceChannelRequest request, CancellationToken cancellationToken = default)
    {
        var channelId = Guid.NewGuid();
        var state = new VoiceChannelState
        {
            ChannelId = channelId,
            Mode = request.Mode,
            ScopeKey = NormalizeScopeKey(request.ScopeKey),
            CreatedAt = DateTime.UtcNow,
            EncryptionToken = request.Mode == VoiceMode.EncryptedPrivate ? CreateEncryptionToken() : string.Empty
        };

        state.Participants[request.CreatorPlayerId] = new VoiceParticipantState
        {
            PlayerId = request.CreatorPlayerId,
            LastActivityAt = DateTime.UtcNow
        };
        state.SignalQueues[request.CreatorPlayerId] = new ConcurrentQueue<VoiceSignalDto>();

        _channels[channelId] = state;
        return Task.FromResult(MapChannel(state));
    }

    public Task<VoiceChannelDto?> GetChannelAsync(Guid channelId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_channels.TryGetValue(channelId, out var channel)
            ? MapChannel(channel)
            : null);
    }

    public Task<VoiceChannelDto?> JoinChannelAsync(Guid channelId, JoinVoiceChannelRequest request, CancellationToken cancellationToken = default)
    {
        if (!_channels.TryGetValue(channelId, out var channel))
        {
            return Task.FromResult<VoiceChannelDto?>(null);
        }

        channel.Participants[request.PlayerId] = new VoiceParticipantState
        {
            PlayerId = request.PlayerId,
            LastActivityAt = DateTime.UtcNow
        };
        channel.SignalQueues.TryAdd(request.PlayerId, new ConcurrentQueue<VoiceSignalDto>());

        return Task.FromResult<VoiceChannelDto?>(MapChannel(channel));
    }

    public Task<bool> LeaveChannelAsync(Guid channelId, Guid playerId, CancellationToken cancellationToken = default)
    {
        if (!_channels.TryGetValue(channelId, out var channel))
        {
            return Task.FromResult(false);
        }

        channel.Participants.TryRemove(playerId, out _);
        channel.SignalQueues.TryRemove(playerId, out _);

        if (channel.Participants.IsEmpty)
        {
            _channels.TryRemove(channelId, out _);
        }

        return Task.FromResult(true);
    }

    public Task<VoiceSignalDto?> PublishSignalAsync(Guid channelId, VoiceSignalRequest request, CancellationToken cancellationToken = default)
    {
        if (!_channels.TryGetValue(channelId, out var channel))
        {
            return Task.FromResult<VoiceSignalDto?>(null);
        }

        if (!channel.Participants.ContainsKey(request.SenderId))
        {
            return Task.FromResult<VoiceSignalDto?>(null);
        }

        var signal = new VoiceSignalDto
        {
            ChannelId = channelId,
            SenderId = request.SenderId,
            TargetPlayerId = request.TargetPlayerId,
            SignalType = string.IsNullOrWhiteSpace(request.SignalType) ? "offer" : request.SignalType.Trim().ToLowerInvariant(),
            Payload = request.Payload,
            CreatedAt = DateTime.UtcNow
        };

        if (request.TargetPlayerId.HasValue)
        {
            if (channel.SignalQueues.TryGetValue(request.TargetPlayerId.Value, out var targetedQueue))
            {
                targetedQueue.Enqueue(signal);
            }
        }
        else
        {
            foreach (var participant in channel.SignalQueues)
            {
                if (participant.Key == request.SenderId)
                {
                    continue;
                }

                participant.Value.Enqueue(signal);
            }
        }

        return Task.FromResult<VoiceSignalDto?>(signal);
    }

    public Task<IReadOnlyList<VoiceSignalDto>> DequeueSignalsAsync(
        Guid channelId,
        Guid playerId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (!_channels.TryGetValue(channelId, out var channel))
        {
            return Task.FromResult<IReadOnlyList<VoiceSignalDto>>([]);
        }

        if (!channel.SignalQueues.TryGetValue(playerId, out var queue))
        {
            return Task.FromResult<IReadOnlyList<VoiceSignalDto>>([]);
        }

        var max = Math.Clamp(limit, 1, 200);
        var signals = new List<VoiceSignalDto>(max);
        while (signals.Count < max && queue.TryDequeue(out var signal))
        {
            signals.Add(signal);
        }

        return Task.FromResult<IReadOnlyList<VoiceSignalDto>>(signals);
    }

    public Task<VoiceActivityDto?> UpdateActivityAsync(Guid channelId, VoiceActivityRequest request, CancellationToken cancellationToken = default)
    {
        if (!_channels.TryGetValue(channelId, out var channel))
        {
            return Task.FromResult<VoiceActivityDto?>(null);
        }

        if (!channel.Participants.TryGetValue(request.PlayerId, out var participant))
        {
            return Task.FromResult<VoiceActivityDto?>(null);
        }

        participant.LastActivityAt = DateTime.UtcNow;
        participant.LastRmsLevel = Math.Clamp(request.RmsLevel, 0f, 1f);
        participant.PacketLossPercent = Math.Clamp(request.PacketLossPercent, 0f, 100f);
        participant.LatencyMs = Math.Max(0f, request.LatencyMs);
        participant.JitterMs = Math.Max(0f, request.JitterMs);
        participant.IsSpeaking = participant.LastRmsLevel >= 0.02f;

        var activity = new VoiceActivityDto
        {
            ChannelId = channelId,
            PlayerId = request.PlayerId,
            IsSpeaking = participant.IsSpeaking,
            VoiceActivityScore = ComputeVoiceActivityScore(participant.LastRmsLevel, participant.PacketLossPercent),
            UpdatedAt = participant.LastActivityAt
        };

        return Task.FromResult<VoiceActivityDto?>(activity);
    }

    public Task<SpatialAudioResult?> CalculateSpatialMixAsync(Guid channelId, SpatialAudioRequest request, CancellationToken cancellationToken = default)
    {
        if (!_channels.TryGetValue(channelId, out var channel))
        {
            return Task.FromResult<SpatialAudioResult?>(null);
        }

        if (!channel.Participants.ContainsKey(request.ListenerId))
        {
            return Task.FromResult<SpatialAudioResult?>(null);
        }

        var falloff = Math.Max(1f, request.FalloffDistance);
        var mix = request.Speakers
            .Where(speaker => speaker.PlayerId != request.ListenerId)
            .Select(speaker =>
            {
                var dx = speaker.X - request.ListenerX;
                var dy = speaker.Y - request.ListenerY;
                var dz = speaker.Z - request.ListenerZ;
                var distance = MathF.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
                var attenuation = 1f / (1f + (distance / falloff));
                var pan = Math.Clamp(dx / falloff, -1f, 1f);
                return new SpeakerMix
                {
                    PlayerId = speaker.PlayerId,
                    Distance = distance,
                    Gain = Math.Clamp(speaker.BaseGain * attenuation, 0f, 1f),
                    Pan = pan
                };
            })
            .OrderBy(m => m.Distance)
            .ToList();

        return Task.FromResult<SpatialAudioResult?>(new SpatialAudioResult
        {
            ListenerId = request.ListenerId,
            Mix = mix
        });
    }

    public Task<VoiceQosSnapshot?> GetQosSnapshotAsync(Guid channelId, CancellationToken cancellationToken = default)
    {
        if (!_channels.TryGetValue(channelId, out var channel))
        {
            return Task.FromResult<VoiceQosSnapshot?>(null);
        }

        var participants = channel.Participants.Values.ToList();
        if (participants.Count == 0)
        {
            return Task.FromResult<VoiceQosSnapshot?>(new VoiceQosSnapshot
            {
                ChannelId = channelId,
                ParticipantCount = 0,
                SampledAt = DateTime.UtcNow
            });
        }

        var snapshot = new VoiceQosSnapshot
        {
            ChannelId = channelId,
            ParticipantCount = participants.Count,
            AverageLatencyMs = participants.Average(p => p.LatencyMs),
            AverageJitterMs = participants.Average(p => p.JitterMs),
            AveragePacketLossPercent = participants.Average(p => p.PacketLossPercent),
            SpeakingParticipants = participants.Count(p => p.IsSpeaking),
            SampledAt = DateTime.UtcNow
        };

        return Task.FromResult<VoiceQosSnapshot?>(snapshot);
    }

    private static string NormalizeScopeKey(string scopeKey)
    {
        return string.IsNullOrWhiteSpace(scopeKey) ? "default" : scopeKey.Trim().ToLowerInvariant();
    }

    private static float ComputeVoiceActivityScore(float rmsLevel, float packetLossPercent)
    {
        var signalStrength = Math.Clamp(rmsLevel * 1.5f, 0f, 1f);
        var packetLossPenalty = Math.Clamp(packetLossPercent / 100f, 0f, 1f);
        return Math.Clamp(signalStrength * (1f - (packetLossPenalty * 0.4f)), 0f, 1f);
    }

    private static string CreateEncryptionToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static VoiceChannelDto MapChannel(VoiceChannelState channel)
    {
        return new VoiceChannelDto
        {
            ChannelId = channel.ChannelId,
            Mode = channel.Mode,
            ScopeKey = channel.ScopeKey,
            EncryptionToken = channel.EncryptionToken,
            ParticipantCount = channel.Participants.Count,
            CreatedAt = channel.CreatedAt
        };
    }

    private sealed class VoiceChannelState
    {
        public Guid ChannelId { get; init; }
        public VoiceMode Mode { get; init; }
        public string ScopeKey { get; init; } = string.Empty;
        public string EncryptionToken { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public ConcurrentDictionary<Guid, VoiceParticipantState> Participants { get; } = new();
        public ConcurrentDictionary<Guid, ConcurrentQueue<VoiceSignalDto>> SignalQueues { get; } = new();
    }

    private sealed class VoiceParticipantState
    {
        public Guid PlayerId { get; init; }
        public DateTime LastActivityAt { get; set; }
        public float LastRmsLevel { get; set; }
        public float PacketLossPercent { get; set; }
        public float LatencyMs { get; set; }
        public float JitterMs { get; set; }
        public bool IsSpeaking { get; set; }
    }
}
