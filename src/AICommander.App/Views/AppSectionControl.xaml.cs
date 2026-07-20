using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AICommander.App.Views;

public partial class AppSectionControl : UserControl
{
    private Point _mouseStartPoint;
    private bool _isDragging = false;
    private ItemsControl _parentItemsControl;
    private int _originalIndex;
    private int _currentIndex;
    private double _itemHeight;

    public AppSectionControl()
    {
        InitializeComponent();
    }

    private void IconImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is Image image)
        {
            try
            {
                image.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/logo.ico"));
            }
            catch { }
        }
    }

    private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        do
        {
            if (current is T ancestor)
                return ancestor;
            current = VisualTreeHelper.GetParent(current);
        } while (current != null);
        return null;
    }

    private void DragHandle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _parentItemsControl = FindAncestor<ItemsControl>(this);
        if (_parentItemsControl == null) return;

        var viewModel = this.DataContext as ViewModels.AppSectionViewModel;
        var mainViewModel = _parentItemsControl.DataContext as ViewModels.MainViewModel;
        if (viewModel == null || mainViewModel == null) return;

        _originalIndex = mainViewModel.AppSections.IndexOf(viewModel);
        _currentIndex = _originalIndex;
        _itemHeight = this.ActualHeight; // Includes margin implicitly due to layout

        _mouseStartPoint = e.GetPosition(_parentItemsControl);

        DragHandle.CaptureMouse();
        _isDragging = true;
        Panel.SetZIndex(this, 1000);

        // Visual indicator that it's lifted
        RootBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212)); // Accent color
        RootBorder.BorderThickness = new Thickness(2);

        // Ensure no animations are running on this item
        DragTransform.BeginAnimation(TranslateTransform.YProperty, null);
    }

    private void DragHandle_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        Point currentMousePoint = e.GetPosition(_parentItemsControl);
        double deltaY = currentMousePoint.Y - _mouseStartPoint.Y;

        // Calculate up and down limits based on position in the list
        double minDeltaY = -_originalIndex * _itemHeight;
        double maxDeltaY = (_parentItemsControl.Items.Count - 1 - _originalIndex) * _itemHeight;

        // Clamp deltaY within the boundaries
        if (deltaY < minDeltaY) deltaY = minDeltaY;
        if (deltaY > maxDeltaY) deltaY = maxDeltaY;

        DragTransform.Y = deltaY;

        int newIndex = _originalIndex + (int)Math.Round(deltaY / _itemHeight);
        var mainViewModel = _parentItemsControl.DataContext as ViewModels.MainViewModel;

        if (newIndex < 0) newIndex = 0;
        if (newIndex >= mainViewModel.AppSections.Count) newIndex = mainViewModel.AppSections.Count - 1;

        if (newIndex != _currentIndex)
        {
            _currentIndex = newIndex;
            ShiftOtherItems();
        }
    }

    private void ShiftOtherItems()
    {
        for (int i = 0; i < _parentItemsControl.Items.Count; i++)
        {
            if (i == _originalIndex) continue;

            if (_parentItemsControl.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement container)
            {
                var contentPresenter = container as ContentPresenter;
                var child = VisualTreeHelper.GetChildrenCount(contentPresenter) > 0 ? VisualTreeHelper.GetChild(contentPresenter, 0) as AppSectionControl : null;

                if (child != null)
                {
                    double shift = 0;

                    if (_originalIndex < _currentIndex && i > _originalIndex && i <= _currentIndex)
                    {
                        shift = -_itemHeight;
                    }
                    else if (_originalIndex > _currentIndex && i < _originalIndex && i >= _currentIndex)
                    {
                        shift = _itemHeight;
                    }

                    child.AnimateTransform(shift);
                }
            }
        }
    }

    public void AnimateTransform(double toY)
    {
        var anim = new DoubleAnimation
        {
            To = toY,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        DragTransform.BeginAnimation(TranslateTransform.YProperty, anim);
    }

    private void DragHandle_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;

        _isDragging = false;
        DragHandle.ReleaseMouseCapture();
        Panel.SetZIndex(this, 0);
        RootBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)); // #E0E0E0
        RootBorder.BorderThickness = new Thickness(1);

        var mainViewModel = _parentItemsControl.DataContext as ViewModels.MainViewModel;

        // Reset all transforms instantly before swapping data context
        DragTransform.BeginAnimation(TranslateTransform.YProperty, null);
        DragTransform.Y = 0;

        for (int i = 0; i < _parentItemsControl.Items.Count; i++)
        {
            if (_parentItemsControl.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement container)
            {
                var contentPresenter = container as ContentPresenter;
                var child = VisualTreeHelper.GetChildrenCount(contentPresenter) > 0 ? VisualTreeHelper.GetChild(contentPresenter, 0) as AppSectionControl : null;
                if (child != null)
                {
                    child.DragTransform.BeginAnimation(TranslateTransform.YProperty, null);
                    child.DragTransform.Y = 0;
                }
            }
        }

        // Apply final move
        if (_originalIndex != _currentIndex && mainViewModel != null)
        {
            mainViewModel.AppSections.Move(_originalIndex, _currentIndex);
        }
    }

    private void ActionOptionsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.IsOpen = true;
        }
    }

    private void ActionNameBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is ViewModels.AppActionViewModel vm)
        {
            vm.IsActionNameFocused = true;
        }
    }

    private void ActionNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is ViewModels.AppActionViewModel vm)
        {
            System.Threading.Tasks.Task.Delay(150).ContinueWith(_ =>
                Dispatcher.Invoke(() => vm.IsActionNameFocused = false));
        }
    }

    private bool _isKeyboardNavigating;

    private void ActionSuggestion_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isKeyboardNavigating) return;

        if (sender is ListBox lb && lb.SelectedItem is ViewModels.ActionSuggestion suggestion && lb.DataContext is ViewModels.AppActionViewModel vm)
        {
            ApplySuggestion(vm, suggestion);
            lb.SelectedItem = null;
        }
    }

    private void ActionNameBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is ViewModels.AppActionViewModel vm)
        {
            if (!vm.IsSuggestionsOpen)
            {
                vm.IsActionNameFocused = true;
                vm.UpdateSuggestions();
            }
        }
    }

    private void ActionNameBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is TextBox tb)
        {
            var parent = tb.Parent as Grid;
            if (parent == null) return;

            System.Windows.Controls.Primitives.Popup popup = null;
            foreach (UIElement child in parent.Children)
            {
                if (child is System.Windows.Controls.Primitives.Popup p)
                {
                    popup = p;
                    break;
                }
            }

            if (popup == null || !popup.IsOpen) return;

            var border = popup.Child as Border;
            var listBox = border?.Child as ListBox;
            if (listBox == null) return;

            if (e.Key == Key.Down)
            {
                e.Handled = true;
                _isKeyboardNavigating = true;
                if (listBox.SelectedIndex < listBox.Items.Count - 1)
                    listBox.SelectedIndex++;
                else if (listBox.SelectedIndex == -1 && listBox.Items.Count > 0)
                    listBox.SelectedIndex = 0;
                if (listBox.SelectedItem != null)
                    listBox.ScrollIntoView(listBox.SelectedItem);
                _isKeyboardNavigating = false;
            }
            else if (e.Key == Key.Up)
            {
                e.Handled = true;
                _isKeyboardNavigating = true;
                if (listBox.SelectedIndex > 0)
                    listBox.SelectedIndex--;
                if (listBox.SelectedItem != null)
                    listBox.ScrollIntoView(listBox.SelectedItem);
                _isKeyboardNavigating = false;
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (listBox.SelectedItem != null && tb.DataContext is ViewModels.AppActionViewModel vm && listBox.SelectedItem is ViewModels.ActionSuggestion suggestion)
                {
                    ApplySuggestion(vm, suggestion);
                    listBox.SelectedItem = null;
                }
            }
        }
    }

    private void ApplySuggestion(ViewModels.AppActionViewModel vm, ViewModels.ActionSuggestion suggestion)
    {
        vm.ActionName = suggestion.Action;
        vm.IsSuggestionsOpen = false;

        // Apply default app hotkey for this template action
        if (suggestion.Template.Equals("Antigravity", System.StringComparison.OrdinalIgnoreCase))
        {
            if (suggestion.Action == "accept") vm.AppHotkey = "ctrl+enter";
            else if (suggestion.Action == "reject") vm.AppHotkey = "escape";
        }
        else if (suggestion.Template.Equals("VS Code", System.StringComparison.OrdinalIgnoreCase) || suggestion.Template.Equals("Code", System.StringComparison.OrdinalIgnoreCase))
        {
            if (suggestion.Action == "accept") vm.AppHotkey = "ctrl+enter";
            else if (suggestion.Action == "inline_chat") vm.AppHotkey = "ctrl+i";
        }
        else if (suggestion.Template.Equals("Claude", System.StringComparison.OrdinalIgnoreCase))
        {
            if (suggestion.Action == "accept") vm.AppHotkey = "ctrl+enter";
        }
    }
}
