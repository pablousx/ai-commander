using System.Windows;
using System.Windows.Controls;
using AICommander.App.ViewModels;

namespace AICommander.App.Views;

public partial class IconSelectionWindow : Window
{
    private MainViewModel _mainViewModel;
    public string SelectedIconPath { get; private set; } = string.Empty;

    public IconSelectionWindow(MainViewModel mainViewModel)
    {
        InitializeComponent();
        _mainViewModel = mainViewModel;
        IconItemsControl.ItemsSource = _mainViewModel.AvailableIcons;
    }

    private void Icon_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is string iconPath)
        {
            SelectedIconPath = iconPath;
            DialogResult = true;
            Close();
        }
    }

    private void AddCustomIcon_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select App Icon",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.ico;*.bmp|All Files|*.*"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _mainViewModel.RegisterIcon(openFileDialog.FileName);
            SelectedIconPath = openFileDialog.FileName;
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
