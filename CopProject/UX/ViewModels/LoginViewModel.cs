using SecureMeet.Shared.Models;
using SecureMeet.Shared.Mvvm;
using UX.Models;

namespace UX.ViewModels;

public sealed class LoginViewModel : ObservableObject
{
    readonly AuthService auth;
    readonly MeetingSession session;

    public LoginViewModel(AuthService auth, MeetingSession session)
    {
        this.auth = auth;
        this.session = session;
        SignInCommand = new RelayCommand(SignIn);

        session.UserSignedOut += Reset;
        Reset();
    }

    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";

    public string UserIdError { get; set; } = "";
    public string EmailError { get; set; } = "";
    public bool HasUserIdError => UserIdError != "";
    public bool HasEmailError => EmailError != "";

    public RelayCommand SignInCommand { get; }

    void Reset()
    {
        UserIdError = "";
        EmailError = "";
        Email = auth.LastUser?.Email ?? "";
        UserId = auth.LastUser?.UserId ?? "";
        Refresh();
    }

    public void AutofillFromEmail()
    {
        UserProfile? known = auth.Find(AuthService.NormalizeEmail(Email));
        if (known != null && UserId.Trim() == "")
        {
            UserId = known.UserId;
            Refresh();
        }
    }

    void SignIn()
    {
        string id = UserId.Trim();
        string mail = AuthService.NormalizeEmail(Email);

        UserIdError = AuthService.UserIdProblem(id);
        EmailError = AuthService.IsValidEmail(mail) ? "" : "Please use your " + AuthService.AllowedDomain + " email address.";

        if (UserIdError == "" && EmailError == "")
        {
            UserProfile? user = auth.SignIn(mail, id, out string problem);
            if (user != null)
            {
                session.SignIn(user);
                return;
            }
            UserIdError = problem;
        }
        Refresh();
    }
}
