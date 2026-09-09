namespace TestZ.Client;

public static class ActivityMonitor
{
    public static int GetIdleSeconds()
    {
        var info = new WinApi.LASTINPUTINFO
        {
            CbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<WinApi.LASTINPUTINFO>()
        };

        if (!WinApi.GetLastInputInfo(ref info))
            return 0; 

        long idleMs = Environment.TickCount64 - info.DwTime;
        return (int)Math.Max(0, idleMs / 1000);
    }
}