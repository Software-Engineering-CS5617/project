// WinUI 3 version of the suggestion overlay. Copy into the actual WinUI project and update
// the namespace/class to match the x:Class value in SuggestionOverlayPanel.xaml.
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WhiteboardSuggestion.Core;

namespace YourApp.Controls;

public sealed class SuggestionCandidateSelectedEventArgs(SuggestionCandidate candidate) : EventArgs
{
    public SuggestionCandidate Candidate { get; } = candidate;
}

public sealed partial class SuggestionOverlayPanel : UserControl
{
    public event EventHandler<SuggestionCandidateSelectedEventArgs>? CandidateSelected;
    public event EventHandler? OverlayClosed;

    public SuggestionOverlayPanel()
    {
        InitializeComponent();
    }

    public void ShowSuggestion(ShapeSuggestion suggestion)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        var candidates = ShapeSuggestionCandidateFactory.Create(suggestion);
        HeaderText.Text = suggestion.Category == DrawingCategory.Unknown
            ? "Possible match"
            : $"Suggested {suggestion.Category.ToString().ToLowerInvariant()}";
        ConfidenceText.Text = $"Model estimate: {suggestion.Confidence:P0} confidence";
        ExplanationText.Text = suggestion.Explanation;
        CandidatesList.ItemsSource = candidates;
        NoCandidatesText.Visibility = candidates.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        Visibility = Visibility.Visible;
    }

    public void Dismiss()
    {
        Visibility = Visibility.Collapsed;
        CandidatesList.ItemsSource = null;
    }

    private void CandidateButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SuggestionCandidate candidate })
        {
            CandidateSelected?.Invoke(this, new SuggestionCandidateSelectedEventArgs(candidate));
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Dismiss();
        OverlayClosed?.Invoke(this, EventArgs.Empty);
    }
}
