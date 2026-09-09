using TestZ.Client;

if (args.Contains("--install"))
{
    Autostart.Install();
    return;
}

if (args.Contains("--uninstall"))
{
    Autostart.Uninstall();
    return;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Logger.Info($"Monitoring client launched ({Environment.UserDomainName}\\{Environment.UserName} @ {Environment.MachineName})");

await new NetworkAgent().RunAsync(cts.Token);

Logger.Info("Client stopped");
