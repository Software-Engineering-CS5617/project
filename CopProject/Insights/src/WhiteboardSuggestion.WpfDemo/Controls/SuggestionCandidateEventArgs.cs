using WhiteboardSuggestion.Core;

namespace WhiteboardSuggestion.WpfDemo.Controls;

public sealed class SuggestionCandidateEventArgs(SuggestionCandidate candidate) : EventArgs
{
    public SuggestionCandidate Candidate { get; } = candidate;
}
