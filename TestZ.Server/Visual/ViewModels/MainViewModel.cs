using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TestZ.Server.ServerEngine;

namespace TestZ.Server.Visual;

public sealed class MainViewModel
{
    private readonly ClientRegistry _registry = new();

    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;

    private readonly CancellationTokenSource _engineCts = new();

    private bool _screenshotBusy;

    public ObservableCollection<ClientRow> Clients { get; } = new();

    public ICommand ScreenshotCommand { get; }

    public MainViewModel()
    {
        ScreenshotCommand = new RelayCommand(
            execute: async p => await TakeScreenshotAsync((ClientRow)p!),
            canExecute: p => p is ClientRow r && r.IsOnline && !_screenshotBusy);

        _registry.RowAdded += row => OnUi(() =>
        {
            if (!Clients.Contains(row))
                Clients.Add(row);
        });

        _ = Task.Run(() => new TcpServer(_registry).RunAsync(_engineCts.Token));

        var sweep = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Settings.HeartbeatIntervalSec) };
        sweep.Tick += (_, _) => _registry.SweepStale();
        sweep.Start();
    }

    private async Task TakeScreenshotAsync(ClientRow row)
    {
        if (_screenshotBusy)
            return;

        ClientSession? session = _registry.GetSession(row);
        if (session is null)
        {
            MessageBox.Show("Client is not online", "Screenshot",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _screenshotBusy = true;
        CommandManager.InvalidateRequerySuggested();
        try
        {
            byte[] jpeg = await session.RequestScreenshotAsync();
            new ScreenshotWindow(row, jpeg).Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Can not take screenshot: {ex.Message}", "Screenshot",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _screenshotBusy = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
private void OnUi(Action action)
    {
        if (_uiContext is not null)
            _uiContext.Post(_ => action(), null);
        else
            action(); 
    }
}