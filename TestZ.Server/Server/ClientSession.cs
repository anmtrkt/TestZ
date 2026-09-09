using System.IO;
using System.Net;
using System.Net.Sockets;
using TestZ.Common;

namespace TestZ.Server.ServerEngine;

public sealed class ClientSession
{
    private readonly TcpClient _tcp;
    private readonly ClientRegistry _registry;

    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private NetworkStream? _stream;

    private TaskCompletionSource<byte[]>? _screenshotTcs;

    public Guid Id { get; } = Guid.NewGuid();

    public string? Key { get; private set; }

    public ClientSession(TcpClient tcp, ClientRegistry registry)
    {
        _tcp = tcp;
        _registry = registry;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _stream = _tcp.GetStream();
        try
        {
            // Handshake
            (MessageType type, byte[] payload) = await Protocol.ReadMessageAsync(_stream, ct);
            if (type != MessageType.Hello)
                throw new IOException($"Ожидался HELLO, получен {type}.");

            HelloInfo hello = Protocol.ReadDto<HelloInfo>(payload)
                              ?? throw new IOException("Не удалось разобрать HELLO.");

            string ip = (_tcp.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "?";
            Key = _registry.RegisterHello(this, hello, ip);
            Logger.Info($"Клиент {Key} ({ip}) зарегистрирован: {hello.User}");

            // Accept Session
            await WriteLockedAsync(MessageType.Welcome,
                Protocol.Serialize(new WelcomeInfo { HeartbeatIntervalSec = Settings.HeartbeatIntervalSec }), ct);

            // Reading cycle
            while (true)
            {
                (type, payload) = await Protocol.ReadMessageAsync(_stream, ct);
                switch (type)
                {
                    case MessageType.Heartbeat:
                        int idle = Protocol.ReadDto<HeartbeatInfo>(payload)?.IdleSeconds ?? 0;
                        _registry.UpdateHeartbeat(Key, idle);
                        break;

                    case MessageType.ScreenshotData:
                       
                        _screenshotTcs?.TrySetResult(payload);
                        break;

                    default:
                        Logger.Info($"Client {Key} send unexpected  message {type}");
                        break;
                }
            }
        }
        finally
        {
            _registry.MarkOffline(this, Key);
            _screenshotTcs?.TrySetCanceled(); 
            _stream.Dispose();
            _tcp.Dispose();
        }
    }

    public async Task<byte[]> RequestScreenshotAsync(CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        Interlocked.Exchange(ref _screenshotTcs, tcs)?.TrySetCanceled();

        try
        {
          
            await WriteLockedAsync(MessageType.ScreenshotRequest, Array.Empty<byte>(), ct);

            Task done = await Task.WhenAny(tcs.Task,
                Task.Delay(TimeSpan.FromSeconds(Settings.ScreenshotTimeoutSec), ct));

            if (done != tcs.Task)
                throw new TimeoutException($"Клиент {Key} не прислал скриншот за {Settings.ScreenshotTimeoutSec} с.");

            return await tcs.Task;
        }
        finally
        {
            Interlocked.CompareExchange(ref _screenshotTcs, null, tcs);
        }
    }

    private async Task WriteLockedAsync(MessageType type, byte[] payload, CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            await Protocol.WriteMessageAsync(_stream!, type, payload, ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}