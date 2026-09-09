namespace TestZ.Server;

public static class Logger
{
    public static void Info(string message)
        => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
}