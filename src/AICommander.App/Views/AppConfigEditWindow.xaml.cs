using System.Windows;
using System.Windows.Controls;
using AICommander.App.ViewModels;

namespace AICommander.App.Views;

public partial class AppConfigEditWindow : Window
{
    public AppSectionViewModel OriginalViewModel { get; }
    public AppSectionViewModel EditViewModel { get; }
    private MainViewModel _mainViewModel;

    public AppConfigEditWindow(AppSectionViewModel viewModel, MainViewModel mainViewModel)
    {
        InitializeComponent();
        _mainViewModel = mainViewModel;

        OriginalViewModel = viewModel;

        // Detached copy for editing
        EditViewModel = new AppSectionViewModel(viewModel.ProviderName, viewModel.IsEnabled, viewModel.ProcessName, viewModel.Icon);

        DataContext = EditViewModel;
    }

    private void IconSelect_Click(object sender, RoutedEventArgs e)
    {
        var iconWindow = new IconSelectionWindow(_mainViewModel);
        iconWindow.Owner = this;
        if (iconWindow.ShowDialog() == true)
        {
            EditViewModel.Icon = iconWindow.SelectedIconPath;
        }
    }

    private void IconImage_ImageFailed(object sender, System.Windows.ExceptionRoutedEventArgs e)
    {
        if (sender is Image image)
        {
            try
            {
                image.Source = new System.Windows.Media.Imaging.BitmapImage(new System.Uri("pack://application:,,,/Assets/logo.ico"));
            }
            catch { }
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EditViewModel.ProviderName) ||
            string.IsNullOrWhiteSpace(EditViewModel.ProcessName) ||
            string.IsNullOrWhiteSpace(EditViewModel.Icon))
        {
            MessageBox.Show("App Name, Process Name, and Icon are mandatory fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _mainViewModel.RegisterIcon(EditViewModel.Icon);

        // Commit changes to original view model
        OriginalViewModel.ProviderName = EditViewModel.ProviderName;
        OriginalViewModel.ProcessName = EditViewModel.ProcessName;
        OriginalViewModel.IsEnabled = EditViewModel.IsEnabled;
        OriginalViewModel.Icon = EditViewModel.Icon;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
