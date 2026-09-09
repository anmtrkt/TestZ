using System.IO;
using System.Net;
using System.Net.Sockets;

namespace TestZ.Server.ServerEngine;

public sealed class TcpServer
{
    private readonly ClientRegistry _registry;

    public TcpServer(ClientRegistry registry) => _registry = registry;

    public async Task RunAsync(CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Any, Settings.Port);
        listener.Start();
        Logger.Info($"Listen 0.0.0.0:{Settings.Port}");

        while (!ct.IsCancellationRequested)
        {
            TcpClient tcp = await listener.AcceptTcpClientAsync(ct);
            Logger.Info($"New connection: {tcp.Client.RemoteEndPoint}");

            var session = new ClientSession(tcp, _registry);
            _ = Task.Run(() => SafeSessionAsync(session, ct), ct);
        }
    }

    private static async Task SafeSessionAsync(ClientSession session, CancellationToken ct)
    {
        try
        {
            await session.RunAsync(ct);
        }
        catch (Exception ex) when (ex is IOException
                                      or SocketException
                                      or ObjectDisposedException
                                      or OperationCanceledException)
        {
            Logger.Info($"Session {session.Key ?? "(without Hello)"} ended: {ex.GetType().Name}");
        }
        catch (Exception ex)
        {
            Logger.Info($"Session {session.Key ?? "(without HELLO)"} exit with unexpected error: {ex}");
        }
    }
}