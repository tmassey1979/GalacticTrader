namespace GalacticTrader.Services.Communication;

public interface IVoiceService
{
    Task<VoiceChannelDto> CreateChannelAsync(CreateVoiceChannelRequest request, CancellationToken cancellationToken = default);
    Task<VoiceChannelDto?> GetChannelAsync(Guid channelId, CancellationToken cancellationToken = default);
    Task<VoiceChannelDto?> JoinChannelAsync(Guid channelId, JoinVoiceChannelRequest request, CancellationToken cancellationToken = default);
    Task<bool> LeaveChannelAsync(Guid channelId, Guid playerId, CancellationToken cancellationToken = default);

    Task<VoiceSignalDto?> PublishSignalAsync(Guid channelId, VoiceSignalRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VoiceSignalDto>> DequeueSignalsAsync(Guid channelId, Guid playerId, int limit = 50, CancellationToken cancellationToken = default);

    Task<VoiceActivityDto?> UpdateActivityAsync(Guid channelId, VoiceActivityRequest request, CancellationToken cancellationToken = default);
    Task<SpatialAudioResult?> CalculateSpatialMixAsync(Guid channelId, SpatialAudioRequest request, CancellationToken cancellationToken = default);
    Task<VoiceQosSnapshot?> GetQosSnapshotAsync(Guid channelId, CancellationToken cancellationToken = default);
}
