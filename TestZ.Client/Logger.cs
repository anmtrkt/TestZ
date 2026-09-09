namespace TestZ.Client;

/// <summary>
/// It can be used for sending to the server
/// </summary>
public static class Logger
{
    public static void Info(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Console.WriteLine(line);
        
    }
}