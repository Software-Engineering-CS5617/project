using UI.Helpers;

namespace UI.ViewModels;

/// <summary>A selectable recipient in the message mention picker.</summary>
public sealed class MentionTargetViewModel : ObservableObject
{
    private readonly Action<MentionTargetViewModel> _toggle;
    private readonly Action<MentionTargetViewModel> _remove;
    private bool _isSelected;

    public MentionTargetViewModel(string username, string displayName, string detail, Action<MentionTargetViewModel> toggle, Action<MentionTargetViewModel> remove)
    {
        Username = username;
        DisplayName = displayName;
        Detail = detail;
        _toggle = toggle;
        _remove = remove;
        ToggleCommand = new RelayCommand(() => _toggle(this));
        RemoveCommand = new RelayCommand(() => _remove(this));
    }

    public string Username { get; }
    public string DisplayName { get; }
    public string Detail { get; }
    public string SelectionMark => IsSelected ? "✓" : "+";
    public bool IsSelected
    {
        get => _isSelected;
        private set
        {
            if (SetProperty(ref _isSelected, value))
                OnPropertyChanged(nameof(SelectionMark));
        }
    }

    public RelayCommand ToggleCommand { get; }
    public RelayCommand RemoveCommand { get; }
    internal void SetSelected(bool selected) => IsSelected = selected;
}
