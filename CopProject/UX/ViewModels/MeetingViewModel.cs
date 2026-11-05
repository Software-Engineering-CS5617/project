using SecureMeet.Shared.Modules;
using SecureMeet.Shared.Mvvm;
using UX.Models;

namespace UX.ViewModels;

public sealed class MeetingViewModel : ObservableObject
{
    readonly MeetingSession session;
    PanelKind tab = PanelKind.Home;
    bool chatOpen;

    public MeetingViewModel(MeetingSession session)
    {
        this.session = session;

        ShowHomeCommand = new RelayCommand(() => ShowTab(PanelKind.Home));
        ShowShareCommand = new RelayCommand(() => ShowTab(PanelKind.Share));
        ShowWhiteboardCommand = new RelayCommand(() => ShowTab(PanelKind.Whiteboard));
        IncidentsCommand = new RelayCommand(() => session.IncidentsOpen = true);

        session.MeetingStarted += _ => OnMeetingStarted();
        session.ParticipantCountChanged += _ => Refresh();
    }

    public string MeetingId => session.Meeting?.Id ?? "";
    public string Port => session.Meeting?.Port ?? "";
    public int Participants => session.ParticipantCount;
    public string Role => session.Meeting?.IsHost == true ? "Host" : "Participant";
    public string EndButtonText => session.Meeting?.IsHost == true ? "End Meeting" : "Leave Meeting";

    public bool IsHomeSelected => tab == PanelKind.Home;
    public bool IsShareSelected => tab == PanelKind.Share;
    public bool IsWhiteboardSelected => tab == PanelKind.Whiteboard;

    public RelayCommand ShowHomeCommand { get; }
    public RelayCommand ShowShareCommand { get; }
    public RelayCommand ShowWhiteboardCommand { get; }
    public RelayCommand IncidentsCommand { get; }

    public bool IsChatOpen
    {
        get => chatOpen;
        set
        {
            chatOpen = value;
            session.NotifyPanelVisibility(PanelKind.Chat, value);
            Refresh();
        }
    }

    void OnMeetingStarted()
    {
        chatOpen = false;
        session.NotifyPanelVisibility(PanelKind.Chat, false);
        ShowTab(PanelKind.Home);
    }

    void ShowTab(PanelKind newTab)
    {
        tab = newTab;
        session.NotifyPanelVisibility(PanelKind.Home, tab == PanelKind.Home);
        session.NotifyPanelVisibility(PanelKind.Share, tab == PanelKind.Share);
        session.NotifyPanelVisibility(PanelKind.Whiteboard, tab == PanelKind.Whiteboard);
        Refresh();
    }

    public void EndMeeting() => session.EndMeeting();
}
