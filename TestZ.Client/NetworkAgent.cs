using System.IO;
using System.Net.Sockets;
using TestZ.Common;

namespace TestZ.Client;

public sealed class NetworkAgent
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>Work until exit from session</summary>
    public async Task RunAsync(CancellationToken ct)
    {
        int delaySec = Settings.ReconnectMinDelaySec;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConnectAndWorkAsync(ct);
                delaySec = Settings.ReconnectMinDelaySec;
            }
            catch (Exception ex) when (ex is IOException
                                          or SocketException
                                          or ObjectDisposedException
                                          or OperationCanceledException)
            {
                Logger.Info($"Connection lost: {ex.Message}");
            }

            Logger.Info($"Retry on {delaySec} s...");
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySec), ct);
            }
            catch (OperationCanceledException) { break; }

            delaySec = Math.Min(delaySec * 2, Settings.ReconnectMaxDelaySec);
        }
    }

    private async Task ConnectAndWorkAsync(CancellationToken ct)
    {
        using var tcp = new TcpClient();
        //Set connection
        Logger.Info($"Connection to {Settings.ServerHost}:{Settings.ServerPort}...");
        await tcp.ConnectAsync(Settings.ServerHost, Settings.ServerPort, ct);
        await using NetworkStream stream = tcp.GetStream();
        Logger.Info("Connection set, send Hello");

        await Protocol.WriteDtoAsync(stream, MessageType.Hello, new HelloInfo
        {
            Domain = Environment.UserDomainName,
            Machine = Environment.MachineName,
            User = Environment.UserName
        }, ct);

        // Waiting ack
        (MessageType type, byte[] payload) = await Protocol.ReadMessageAsync(stream, ct);
        if (type != MessageType.Welcome)
            throw new IOException($"awaitng Welcome but recieve{type}");

        int heartbeatSec = Protocol.ReadDto<WelcomeInfo>(payload)?.HeartbeatIntervalSec
                           ?? Settings.HeartbeatIntervalSec;
        Logger.Info($"Registered. Heartbeat evere {heartbeatSec} s");

        // two parallel cycles, exit if one of them crashes
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Task heartbeat = HeartbeatLoopAsync(stream, heartbeatSec, linked.Token);
        Task commands = CommandLoopAsync(stream, linked.Token);

        Task dead = await Task.WhenAny(heartbeat, commands);
        linked.Cancel();
        await dead; // reconnect
    }

    /// <summary>Пульс: keep-alive + user activity</summary>
    private async Task HeartbeatLoopAsync(NetworkStream stream, int intervalSec, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSec));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var dto = new HeartbeatInfo { IdleSeconds = ActivityMonitor.GetIdleSeconds() };
            await WriteLockedAsync(stream, MessageType.Heartbeat, Protocol.Serialize(dto), ct);
        }
    }

    /// <summary>Recieving server commands</summary>
    private async Task CommandLoopAsync(NetworkStream stream, CancellationToken ct)
    {
        while (true)
        {
            (MessageType type, byte[] payload) = await Protocol.ReadMessageAsync(stream, ct);
            switch (type)
            {
                case MessageType.ScreenshotRequest:
                    Logger.Info("Recieved screenshot command");
                    byte[] jpeg = ScreenshotCapture.CaptureJpeg(); 
                    Logger.Info($"Screenshot {jpeg.Length / 1024} kb, taken");
                    await WriteLockedAsync(stream, MessageType.ScreenshotData, jpeg, ct);
                    break;

                default:
                    Logger.Info($"Unexpected message {type}");
                    break;
            }
        }
    }
    private async Task WriteLockedAsync(NetworkStream stream, MessageType type, byte[] payload, CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            await Protocol.WriteMessageAsync(stream, type, payload, ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
