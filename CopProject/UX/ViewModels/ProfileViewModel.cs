using SecureMeet.Shared.Models;
using SecureMeet.Shared.Mvvm;
using UX.Models;

namespace UX.ViewModels;

public sealed class ProfileViewModel : ObservableObject
{
    readonly AuthService auth;
    readonly MeetingSession session;

    public ProfileViewModel(AuthService auth, MeetingSession session)
    {
        this.auth = auth;
        this.session = session;
    }

    public string Email { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Error { get; set; } = "";
    public bool HasError => Error != "";

    public void Load()
    {
        Email = session.CurrentUser?.Email ?? "";
        UserId = session.CurrentUser?.UserId ?? "";
        Error = "";
        Refresh();
    }

    public bool TrySave()
    {
        string id = UserId.Trim();
        Error = AuthService.UserIdProblem(id);

        if (Error == "")
        {
            UserProfile? user = auth.SignIn(Email, id, out string problem);
            if (user != null)
            {
                session.UpdateProfile(user);
                return true;
            }
            Error = problem;
        }
        Refresh();
        return false;
    }
}
