using System.Net.WebSockets;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GalacticTrader.Desktop.Realtime;

public sealed class CommunicationRealtimeStreamClient
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

    public CommunicationRealtimeStreamClient(
        string baseUrl,
        string accessToken,
        RealtimeReconnectPolicy? reconnectPolicy = null)
    {
        _baseUri = new Uri(baseUrl);
        _accessToken = accessToken;
        _reconnectPolicy = reconnectPolicy ?? new RealtimeReconnectPolicy();
    }

    public event Action<CommunicationRealtimeMessageApiDto>? MessageReceived;
    public event Action<Exception>? ConnectionFaulted;

    public void Start(Guid playerId, string channelType = "global", string channelKey = "desktop-feed")
    {
        lock (_gate)
        {
            if (_runLoopTask is not null)
            {
                return;
            }

            _shutdown = new CancellationTokenSource();
            _runLoopTask = Task.Run(() => RunLoopAsync(playerId, channelType, channelKey, _shutdown.Token));
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

    private async Task RunLoopAsync(
        Guid playerId,
        string channelType,
        string channelKey,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunConnectionAsync(playerId, channelType, channelKey, cancellationToken);
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

    private async Task RunConnectionAsync(
        Guid playerId,
        string channelType,
        string channelKey,
        CancellationToken cancellationToken)
    {
        using var socket = new ClientWebSocket();
        if (!string.IsNullOrWhiteSpace(_accessToken))
        {
            socket.Options.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
        }

        var uri = BuildUri(playerId, channelType, channelKey);
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
            IReadOnlyList<CommunicationRealtimeMessageApiDto> messages;
            try
            {
                messages = ParsePayload(json);
            }
            catch (JsonException)
            {
                continue;
            }

            foreach (var message in messages)
            {
                MessageReceived?.Invoke(message);
            }
        }

        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
        }
    }

    private Uri BuildUri(Guid playerId, string channelType, string channelKey)
    {
        var builder = new UriBuilder(_baseUri);
        builder.Scheme = builder.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
        if (_baseUri.IsDefaultPort)
        {
            builder.Port = -1;
        }

        var basePath = _baseUri.AbsolutePath.TrimEnd('/');
        var path = $"{basePath}/api/communication/ws/{channelType}/{channelKey}";
        builder.Path = path.StartsWith('/') ? path : "/" + path;
        builder.Query = $"playerId={playerId:D}";
        return builder.Uri;
    }

    private static IReadOnlyList<CommunicationRealtimeMessageApiDto> ParsePayload(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            var messages = new List<CommunicationRealtimeMessageApiDto>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                var message = element.Deserialize<CommunicationRealtimeMessageApiDto>(SerializerOptions);
                if (message is not null)
                {
                    messages.Add(message);
                }
            }

            return messages;
        }

        if (document.RootElement.ValueKind == JsonValueKind.Object &&
            document.RootElement.TryGetProperty("id", out _))
        {
            var message = document.RootElement.Deserialize<CommunicationRealtimeMessageApiDto>(SerializerOptions);
            return message is null ? [] : [message];
        }

        return [];
    }
}
