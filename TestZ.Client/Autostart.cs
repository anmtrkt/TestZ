using Microsoft.Win32;

namespace TestZ.Client;

public static class Autostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void Install()
    {
        string exe = Environment.ProcessPath
                     ?? throw new InvalidOperationException("Cannot find path to exe file");

        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        key.SetValue(Settings.AutostartValueName, $"\"{exe}\"");
        Logger.Info($"Autorun writed HKCU\\{RunKey}\\{Settings.AutostartValueName} = \"{exe}\"");
    }
    public static void Uninstall()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(Settings.AutostartValueName, throwOnMissingValue: false);
        Logger.Info("Autorun delited");
    }
}
