using System.ComponentModel;
using System.Windows;

namespace AICommander.App;

public partial class MainWindow : Window
{
    private readonly System.IServiceProvider _serviceProvider;

    public MainWindow(ViewModels.MainViewModel viewModel, System.IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = viewModel;
        _serviceProvider = serviceProvider;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = _serviceProvider.GetService(typeof(SettingsWindow)) as SettingsWindow;
        if (settingsWindow != null)
        {
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }
    }

    private void Hide_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Instead of closing the application, just hide the window
        e.Cancel = true;
        Hide();
    }

    private void AddAppButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.IsOpen = true;
        }
    }
}
