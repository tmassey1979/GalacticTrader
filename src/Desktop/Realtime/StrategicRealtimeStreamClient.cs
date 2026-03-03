using System.Net.WebSockets;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GalacticTrader.Desktop.Realtime;

public sealed class StrategicRealtimeStreamClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Uri _baseUri;
    private readonly string _accessToken;
    private readonly RealtimeReconnectPolicy _reconnectPolicy;
    private readonly object _gate = new();

    private CancellationTokenSource? _shutdown;
    private Task? _runLoopTask;

    public StrategicRealtimeStreamClient(
        string baseUrl,
        string accessToken,
        RealtimeReconnectPolicy? reconnectPolicy = null)
    {
        _baseUri = new Uri(baseUrl);
        _accessToken = accessToken;
        _reconnectPolicy = reconnectPolicy ?? new RealtimeReconnectPolicy();
    }

    public event Action<DashboardRealtimeSnapshotApiDto>? SnapshotReceived;
    public event Action<Exception>? ConnectionFaulted;

    public void Start(Guid playerId, int intervalSeconds = 5)
    {
        lock (_gate)
        {
            if (_runLoopTask is not null)
            {
                return;
            }

            _shutdown = new CancellationTokenSource();
            _runLoopTask = Task.Run(() => RunLoopAsync(playerId, intervalSeconds, _shutdown.Token));
        }
    }

    public async Task StopAsync()
    {
        Task? runLoop;
        CancellationTokenSource? shutdown;
        lock (_gate)
        {
            runLoop = _runLoopTask;
            shutdown = _shutdown;
            _runLoopTask = null;
            _shutdown = null;
        }

        if (shutdown is null || runLoop is null)
        {
            return;
        }

        shutdown.Cancel();
        try
        {
            await runLoop;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            shutdown.Dispose();
        }
    }

    private async Task RunLoopAsync(Guid playerId, int intervalSeconds, CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunConnectionAsync(playerId, intervalSeconds, cancellationToken);
                attempt = 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                ConnectionFaulted?.Invoke(exception);
                attempt++;
                var delay = _reconnectPolicy.GetDelay(attempt);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private async Task RunConnectionAsync(Guid playerId, int intervalSeconds, CancellationToken cancellationToken)
    {
        using var socket = new ClientWebSocket();
        if (!string.IsNullOrWhiteSpace(_accessToken))
        {
            socket.Options.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
        }

        var uri = BuildUri(playerId, intervalSeconds);
        await socket.ConnectAsync(uri, cancellationToken);

        var buffer = new byte[8192];
        using var messageBuffer = new MemoryStream();

        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                continue;
            }

            messageBuffer.Write(buffer, 0, result.Count);
            if (!result.EndOfMessage)
            {
                continue;
            }

            var json = Encoding.UTF8.GetString(messageBuffer.GetBuffer(), 0, (int)messageBuffer.Length);
            messageBuffer.SetLength(0);

            DashboardRealtimeSnapshotApiDto? snapshot;
            try
            {
                snapshot = JsonSerializer.Deserialize<DashboardRealtimeSnapshotApiDto>(json, SerializerOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (snapshot is not null)
            {
                SnapshotReceived?.Invoke(snapshot);
            }
        }

        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
        }
    }

    private Uri BuildUri(Guid playerId, int intervalSeconds)
    {
        var builder = new UriBuilder(_baseUri);
        builder.Scheme = builder.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
        if (_baseUri.IsDefaultPort)
        {
            builder.Port = -1;
        }

        var basePath = _baseUri.AbsolutePath.TrimEnd('/');
        var path = $"{basePath}/api/strategic/ws/dashboard/{playerId:D}";
        builder.Path = path.StartsWith('/') ? path : "/" + path;
        builder.Query = $"intervalSeconds={Math.Clamp(intervalSeconds, 2, 30)}";
        return builder.Uri;
    }
}
