using System.Windows;
using TestZ.Server.Visual;

namespace TestZ.Server;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}