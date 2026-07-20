namespace AICommander.App.ViewModels;

public class AppActionViewModel : ViewModelBase
{
    private string _actionName;
    private string _globalHotkey;
    private string _appHotkey;

    private int _index;

    public AppActionViewModel(string actionName, string globalHotkey, string appHotkey)
    {
        _actionName = actionName;
        _globalHotkey = globalHotkey;
        _appHotkey = appHotkey;
    }

    public int Index
    {
        get => _index;
        set => SetProperty(ref _index, value);
    }

    public string ActionName
    {
        get => _actionName;
        set
        {
            if (SetProperty(ref _actionName, value))
                UpdateSuggestions();
        }
    }

    public string GlobalHotkey
    {
        get => _globalHotkey;
        set => SetProperty(ref _globalHotkey, value);
    }

    public string AppHotkey
    {
        get => _appHotkey;
        set => SetProperty(ref _appHotkey, value);
    }

    private bool _isDuplicateGlobalHotkey;
    public bool IsDuplicateGlobalHotkey
    {
        get => _isDuplicateGlobalHotkey;
        set => SetProperty(ref _isDuplicateGlobalHotkey, value);
    }

    private bool _isSystemGlobalHotkeyConflict;
    public bool IsSystemGlobalHotkeyConflict
    {
        get => _isSystemGlobalHotkeyConflict;
        set => SetProperty(ref _isSystemGlobalHotkeyConflict, value);
    }

    private bool _isActionNameFocused;
    public bool IsActionNameFocused
    {
        get => _isActionNameFocused;
        set
        {
            if (SetProperty(ref _isActionNameFocused, value))
                UpdateSuggestions();
        }
    }

    private bool _isSuggestionsOpen;
    public bool IsSuggestionsOpen
    {
        get => _isSuggestionsOpen;
        set => SetProperty(ref _isSuggestionsOpen, value);
    }

    private System.Collections.ObjectModel.ObservableCollection<ActionSuggestion> _suggestedActions = new();
    public System.Collections.ObjectModel.ObservableCollection<ActionSuggestion> SuggestedActions
    {
        get => _suggestedActions;
        set => SetProperty(ref _suggestedActions, value);
    }

    private System.Windows.Data.ListCollectionView? _suggestedActionsView;
    public System.Windows.Data.ListCollectionView SuggestedActionsView
    {
        get
        {
            if (_suggestedActionsView == null)
            {
                _suggestedActionsView = new System.Windows.Data.ListCollectionView(SuggestedActions);
                _suggestedActionsView.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription("Template"));
            }
            return _suggestedActionsView;
        }
    }

    public void UpdateSuggestions()
    {
        SuggestedActions.Clear();

        if (!IsActionNameFocused)
        {
            IsSuggestionsOpen = false;
            return;
        }

        string filter = ActionName?.ToLowerInvariant() ?? "";
        bool hasSuggestions = false;

        foreach (var kvp in MainViewModel.KnownTemplates)
        {
            foreach (var action in kvp.Value)
            {
                if (string.IsNullOrWhiteSpace(filter) || action.ToLowerInvariant().Contains(filter))
                {
                    SuggestedActions.Add(new ActionSuggestion { Template = kvp.Key, Action = action });
                    hasSuggestions = true;
                }
            }
        }

        IsSuggestionsOpen = hasSuggestions;
    }
}

public class ActionSuggestion
{
    public string Template { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}
