namespace TestZ.Client;

public static class Settings
{
    public const string ServerHost = "127.0.0.1";

    public const int ServerPort = 4545;

    public const int HeartbeatIntervalSec = 5;

    public const int ReconnectMinDelaySec = 2;

    public const int ReconnectMaxDelaySec = 30;

    /// <summary>
    /// max jpeg size in mb
    /// </summary>
    public const int JpegQuality = 70;

    public const string AutostartValueName = "MonitorClient";
}
