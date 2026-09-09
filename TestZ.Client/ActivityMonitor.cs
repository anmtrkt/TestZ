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
        uint now = unchecked((uint)Environment.TickCount);
        uint idleMs = unchecked(now - info.DwTime);
        return (int)(idleMs / 1000);
    }
}
