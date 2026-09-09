using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace TestZ.Server;

public partial class ScreenshotWindow : Window
{
    public ScreenshotWindow(ClientRow row, byte[] jpeg)
    {
        InitializeComponent();
        Title = $"Screenshot — {row.Machine}\\{row.User} ({row.Ip})";

        var image = new BitmapImage();
        using var ms = new MemoryStream(jpeg);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();

        ScreenshotImage.Source = image;
    }
}