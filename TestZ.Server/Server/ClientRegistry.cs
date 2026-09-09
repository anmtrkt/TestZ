using System.Collections.Concurrent;
using TestZ.Common;

namespace TestZ.Server.ServerEngine;
public sealed class ClientRegistry
{
    private readonly ConcurrentDictionary<string, ClientRow> _rows = new();
    private readonly ConcurrentDictionary<string, ClientSession> _sessions = new();

    public event Action<ClientRow>? RowAdded;

    public string RegisterHello(ClientSession session, HelloInfo hello, string ip)
    {
        string key = $"{hello.Machine}\\{hello.User}";
        ClientRow row = _rows.GetOrAdd(key, _ => new ClientRow());
        row.ApplyHello(hello.Domain, hello.Machine, hello.User, ip, session.Id);
        _sessions[key] = session;
        RowAdded?.Invoke(row);
        return key;
    }

    public void UpdateHeartbeat(string key, int idleSeconds)
        => _rows.GetValueOrDefault(key)?.ApplyHeartbeat(idleSeconds);

    public void MarkOffline(ClientSession session, string? key)
    {
        if (key is null)
            return; 
        if (_sessions.TryGetValue(key, out ClientSession? current) && ReferenceEquals(current, session))
            _sessions.TryRemove(key, out _);

        if (_rows.GetValueOrDefault(key) is { } row && row.CurrentSessionId == session.Id)
            row.ApplyOffline();
    }

    public void SweepStale()
    {
        TimeSpan threshold =
            TimeSpan.FromSeconds(Settings.HeartbeatIntervalSec * Settings.OfflineAfterMissedBeats);

        foreach ((string key, ClientRow row) in _rows)
        {
            if (row.Status != ClientStatus.Offline && DateTime.Now - row.LastSeen > threshold)
            {
                _sessions.TryRemove(key, out _); 
                row.ApplyOffline();
                Logger.Info($"Client {key} client is offline by heartbeat timeout");
            }
        }
    }

    public ClientSession? GetSession(ClientRow row)
        => _sessions.GetValueOrDefault($"{row.Machine}\\{row.User}");
}