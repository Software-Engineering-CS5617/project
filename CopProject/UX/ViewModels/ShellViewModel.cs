using SecureMeet.Shared.Mvvm;
using UX.Models;

namespace UX.ViewModels;

public sealed class ShellViewModel : ObservableObject
{
    readonly MeetingSession session;

    public ShellViewModel(MeetingSession session, LoginViewModel login, DashboardViewModel dashboard,
                          MeetingViewModel meeting, ProfileViewModel profile)
    {
        this.session = session;
        Login = login;
        Dashboard = dashboard;
        Meeting = meeting;
        Profile = profile;

        SignOutCommand = new RelayCommand(session.SignOut);
        BackCommand = new RelayCommand(() => session.IncidentsOpen = false);

        session.Changed += Refresh;
        session.ProfileChanged += _ => Refresh();
    }

    public LoginViewModel Login { get; }
    public DashboardViewModel Dashboard { get; }
    public MeetingViewModel Meeting { get; }
    public ProfileViewModel Profile { get; }

    bool SignedIn => session.CurrentUser != null;
    public bool IsLoginVisible => !SignedIn;
    public bool IsIncidentsVisible => SignedIn && session.IncidentsOpen;
    public bool IsMeetingVisible => SignedIn && session.InMeeting && !session.IncidentsOpen;
    public bool IsDashboardVisible => SignedIn && !session.InMeeting && !session.IncidentsOpen;

    public bool IsUserPanelVisible => SignedIn;
    public string ProfileName => session.CurrentUser?.UserId ?? "";
    public string MeetingTitle => session.Meeting?.IsHost == true ? "Meeting (Host)" : "Meeting";

    public RelayCommand SignOutCommand { get; }
    public RelayCommand BackCommand { get; }
}
