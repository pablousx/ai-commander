using System.Collections.ObjectModel;
using System.Windows.Input;

namespace AICommander.App.ViewModels;

public class AppSectionViewModel : ViewModelBase
{
    private string _providerName;
    private bool _isEnabled;
    private string _processName;
    private string _icon;

    public ObservableCollection<AppActionViewModel> Actions { get; }

    public ICommand AddActionCommand { get; }
    public ICommand RemoveActionCommand { get; }

    public AppSectionViewModel(string providerName, bool isEnabled, string processName, string icon = "")
    {
        _providerName = providerName;
        _isEnabled = isEnabled;
        _processName = processName;
        _icon = icon;

        Actions = new ObservableCollection<AppActionViewModel>();
        Actions.CollectionChanged += (s, e) => ReindexActions();

        AddActionCommand = new RelayCommand(_ =>
        {
            Actions.Add(new AppActionViewModel("", "None", "None"));
        });

        RemoveActionCommand = new RelayCommand<AppActionViewModel>(action =>
        {
            if (action != null)
            {
                Actions.Remove(action);
            }
        });

        ResetActionCommand = new RelayCommand<AppActionViewModel>(action =>
        {
            if (action == null) return;

            string actionName = action.ActionName?.ToLowerInvariant() ?? "";
            string providerName = _providerName?.ToLowerInvariant() ?? "";

            action.GlobalHotkey = "unset";
            action.AppHotkey = "unset";

            if (providerName == "antigravity")
            {
                if (actionName == "accept") action.AppHotkey = "ctrl+enter";
                else if (actionName == "reject") action.AppHotkey = "escape";
            }
            else if (providerName == "vscode" || providerName == "code")
            {
                if (actionName == "accept") action.AppHotkey = "ctrl+enter";
                else if (actionName == "inline_chat") action.AppHotkey = "ctrl+i";
            }
            else if (providerName == "claude")
            {
                if (actionName == "accept") action.AppHotkey = "ctrl+enter";
            }
        });
    }

    public ICommand ResetActionCommand { get; }

    public string ProviderName
    {
        get => _providerName;
        set => SetProperty(ref _providerName, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string ProcessName
    {
        get => _processName;
        set => SetProperty(ref _processName, value);
    }

    public string Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    private void ReindexActions()
    {
        for (int i = 0; i < Actions.Count; i++)
        {
            Actions[i].Index = i + 1;
        }
    }
}
