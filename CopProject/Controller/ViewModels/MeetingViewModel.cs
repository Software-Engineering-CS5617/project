using System.Collections.ObjectModel;
using System.Linq;
using Controller.Models;

namespace Controller.ViewModels;

public sealed class MeetingViewModel : ObservableObject
{
    readonly MeetingSession session;

    public MeetingViewModel(MeetingSession session)
    {
        this.session = session;

        session.MeetingStarted += _ => OnMeetingStarted();
        session.ParticipantCountChanged += _ => Refresh();
        session.ParticipantsChanged += list =>
        {
            ParticipantList.Clear();
            foreach (var p in list)
            {
                ParticipantList.Add(p);
            }

            if (SelectedParticipant != null)
            {
                SelectedParticipant = ParticipantList.FirstOrDefault(p => p.Username == SelectedParticipant.Username);
            }
            Refresh();
        };
    }

    public string MeetingId => session.Meeting?.Id ?? "";
    public string Port => session.Meeting?.Port ?? "";
    public int Participants => session.ParticipantCount;
    public string Role => session.Meeting?.IsHost == true ? "Host" : "Participant";

    public ObservableCollection<ParticipantInfo> ParticipantList { get; } = new();

    private ParticipantInfo? _selectedParticipant;
    public ParticipantInfo? SelectedParticipant
    {
        get => _selectedParticipant;
        set
        {
            if (_selectedParticipant == value) return;
            _selectedParticipant = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedParticipant));
        }
    }

    public bool HasSelectedParticipant => SelectedParticipant != null;

    void OnMeetingStarted()
    {
        ParticipantList.Clear();
        SelectedParticipant = null;
        Refresh();
    }
}