using System.Windows;
using System.Windows.Controls;
using WhiteboardSuggestion.Core;

namespace WhiteboardSuggestion.WpfDemo.Controls;

/// <summary>
/// Floating candidate panel. The host decides what to do when a candidate is clicked.
/// </summary>
public partial class SuggestionOverlayControl : UserControl
{
    public event EventHandler<SuggestionCandidateEventArgs>? CandidateSelected;
    public event EventHandler? OverlayClosed;

    public SuggestionOverlayControl()
    {
        InitializeComponent();
    }

    public void ShowSuggestion(ShapeSuggestion suggestion)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        IReadOnlyList<SuggestionCandidate> candidates = ShapeSuggestionCandidateFactory.Create(suggestion);
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
            CandidateSelected?.Invoke(this, new SuggestionCandidateEventArgs(candidate));
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Dismiss();
        OverlayClosed?.Invoke(this, EventArgs.Empty);
    }
}
