using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestZ.Server;

public enum ClientStatus
{
    Online,

    Away,

    Offline,
}

public sealed class ClientRow : INotifyPropertyChanged
{
    private string _domain = "";
    private string _machine = "";
    private string _user = "";
    private string _ip = "";
    private DateTime _lastSeen;
    private int _idleSeconds;
    private ClientStatus _status;

    internal Guid CurrentSessionId { get; set; }

    public string Domain { get => _domain; private set => Set(ref _domain, value); }
    public string Machine { get => _machine; private set => Set(ref _machine, value); }
    public string User { get => _user; private set => Set(ref _user, value); }
    public string Ip { get => _ip; private set => Set(ref _ip, value); }
public DateTime LastSeen { get => _lastSeen; private set => Set(ref _lastSeen, value); }

    public int IdleSeconds
    {
        get => _idleSeconds;
        private set
        {
            if (Set(ref _idleSeconds, value))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IdleText)));
        }
    }

    public ClientStatus Status
    {
        get => _status;
        private set
        {
            if (Set(ref _status, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOnline)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IdleText)));
            }
        }
    }

    public string Key => $"{Machine}\\{User}";

    public bool IsOnline => Status != ClientStatus.Offline;
    public string IdleText => Status == ClientStatus.Offline
        ? "—"
        : IdleSeconds < 60 ? $"{IdleSeconds} с" : $"{IdleSeconds / 60} мин";


    internal void ApplyHello(string domain, string machine, string user,
        string ip, Guid sessionId)
    {
        Domain = domain;
        Machine = machine;
        User = user;
        Ip = ip;
        CurrentSessionId = sessionId;
        LastSeen = DateTime.Now;
        Status = ClientStatus.Online;
    }

    internal void ApplyHeartbeat(int idleSeconds)
    {
        LastSeen = DateTime.Now;
        IdleSeconds = idleSeconds;
        Status = idleSeconds >= Settings.AwayAfterIdleSec ? ClientStatus.Away : ClientStatus.Online;
    }

    internal void ApplyOffline() => Status = ClientStatus.Offline;

    public event PropertyChangedEventHandler? PropertyChanged;
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}