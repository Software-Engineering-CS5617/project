using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using UI.Helpers;

namespace UI.ViewModels;

/// <summary>
/// Shared avatar presentation for participants and messages.
/// The meeting/profile component may supply an ImageSource; otherwise initials are shown.
/// All avatar image sources must be assigned on the UI thread.
/// </summary>
public abstract class AvatarViewModel : ObservableObject
{
    private static readonly Color[] AvatarPalette =
    [
        Color.FromArgb(255, 72, 107, 145),
        Color.FromArgb(255, 65, 126, 111),
        Color.FromArgb(255, 133, 91, 137),
        Color.FromArgb(255, 150, 105, 67),
        Color.FromArgb(255, 91, 111, 145),
        Color.FromArgb(255, 137, 111, 72)
    ];

    private ImageSource? _avatarImageSource;
    private Brush _avatarBrush;

    protected AvatarViewModel(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        DisplayName = displayName;
        Initials = GetInitials(displayName);
        _avatarBrush = new SolidColorBrush(GetAvatarColor(displayName));
    }

    public string DisplayName { get; }

    public string Initials { get; }

    public ImageSource? AvatarImageSource => _avatarImageSource;

    /// <summary>
    /// Brush used by the circular avatar. It is an ImageBrush when a profile image
    /// is available and a deterministic solid color otherwise.
    /// </summary>
    public Brush AvatarBrush
    {
        get => _avatarBrush;
        private set => SetProperty(ref _avatarBrush, value);
    }

    public Visibility InitialsVisibility =>
        _avatarImageSource is null ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Sets or clears the profile image for this avatar.
    /// Call from the UI thread because ImageSource is a WinUI object.
    /// </summary>
    public void SetAvatarImage(ImageSource? image)
    {
        _avatarImageSource = image;

        AvatarBrush = image is null
            ? new SolidColorBrush(GetAvatarColor(DisplayName))
            : new ImageBrush
            {
                ImageSource = image,
                Stretch = Stretch.UniformToFill
            };

        OnPropertyChanged(nameof(AvatarImageSource));
        OnPropertyChanged(nameof(InitialsVisibility));
    }

    private static string GetInitials(string name)
    {
        string[] parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return "?";

        if (parts.Length == 1)
            return parts[0][..1].ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static Color GetAvatarColor(string name)
    {
        // String.GetHashCode is process-randomized; use a stable small hash instead.
        int hash = 17;
        foreach (char character in name)
            hash = unchecked(hash * 31 + character);

        return AvatarPalette[(hash & 0x7fffffff) % AvatarPalette.Length];
    }
}
