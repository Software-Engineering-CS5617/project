using System.Collections.ObjectModel;
using UI.Helpers;

namespace UI.ViewModels;

/// <summary>
/// UI representation of one emoji reaction and the participants who selected it.
/// Clicking the chip toggles the current participant's reaction.
/// </summary>
public sealed class ReactionViewModel : ObservableObject
{
    private readonly string _currentUsername;
    private readonly Action<string>? _toggleReaction;

    public ReactionViewModel(
        string emoji,
        IEnumerable<string> users,
        string currentUsername,
        Action<string>? toggleReaction = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emoji);
        ArgumentNullException.ThrowIfNull(users);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUsername);

        Emoji = emoji;
        _currentUsername = currentUsername;
        _toggleReaction = toggleReaction;
        Users = new ObservableCollection<string>(users.Distinct(StringComparer.Ordinal));

        ToggleCommand = new RelayCommand(
            () => _toggleReaction?.Invoke(Emoji),
            () => _toggleReaction is not null);
    }

    public string Emoji { get; }

    public ObservableCollection<string> Users { get; }

    public int Count => Users.Count;

    public string Label => $"{Emoji} {Count}";

    public string Tooltip => string.Join(", ", Users);

    public bool IsReactedByCurrentUser => Users.Contains(_currentUsername);

    public RelayCommand ToggleCommand { get; }

    /// <summary>
    /// Applies a reaction toggle received from the backend.
    /// This method is called by the parent message ViewModel on the UI thread.
    /// </summary>
    internal void ToggleUser(string username)
    {
        int index = Users.IndexOf(username);

        if (index >= 0)
            Users.RemoveAt(index);
        else
            Users.Add(username);

        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Tooltip));
        OnPropertyChanged(nameof(IsReactedByCurrentUser));
    }
}
