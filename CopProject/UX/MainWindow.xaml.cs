using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace App2;

public sealed partial class MainWindow : Window
{
    // list shown in "Previous Meeting Records"
    ObservableCollection<string> records = new ObservableCollection<string>()
    {
        "Weekly Review - Mon, 28 Sep - 45 min",
        "Patrol Planning - Fri, 25 Sep - 30 min",
        "Case Briefing - Wed, 23 Sep - 1 hr 10 min"
    };

    // accounts and the remembered email are saved here
    ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;

    // only college emails can use the app
    const string AllowedDomain = "@smail.iitpkd.ac.in";

    string meetingTitle = "";
    string meetingId = "";
    string meetingPort = "";
    bool resizingChat = false;
    int failedAttempts = 0;
    DateTime lockedUntil = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();
        Title = "SecureMeet";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
        RecordsList.ItemsSource = records;
        ShowScreen("login");
    }

    // ---------------- helpers ----------------

    Visibility Vis(bool show) { return show ? Visibility.Visible : Visibility.Collapsed; }

    // only one screen is visible at a time
    void ShowScreen(string screen)
    {
        LoginView.Visibility = Vis(screen == "login");
        DashView.Visibility = Vis(screen == "dash");
        MeetingView.Visibility = Vis(screen == "meeting");
        UserPanel.Visibility = Vis(screen != "login");
        MeetingTitleText.Visibility = Vis(screen == "meeting");

        if (screen == "login")
        {
            ClearMessages();
            EmailBox.Text = (string)settings.Values["rememberedEmail"] ?? "";
            RememberCheck.IsChecked = EmailBox.Text != "";
            PassBox.Password = "";
        }
    }

    void ClearMessages()
    {
        foreach (TextBlock t in new[] { LoginInfo, LoginError, EmailError, PassError })
            t.Visibility = Visibility.Collapsed;
    }

    void ShowMessage(TextBlock label, string text)
    {
        label.Text = text;
        label.Visibility = Visibility.Visible;
    }

    ContentDialog MakeDialog(string title, object content, string okText)
    {
        return new ContentDialog { Title = title, Content = content, PrimaryButtonText = okText, CloseButtonText = "Cancel", XamlRoot = Root.XamlRoot };
    }

    TextBlock MakeError()
    {
        return new TextBlock { Foreground = new SolidColorBrush(Colors.Red), TextWrapping = TextWrapping.Wrap };
    }

    // ---------------- accounts and passwords ----------------

    bool IsValidEmail(string email)
    {
        if (!email.EndsWith(AllowedDomain)) return false;
        try { return new MailAddress(email).Address == email; }
        catch { return false; }
    }

    // the port must be a number between 1 and 65535
    bool IsValidPort(string port)
    {
        return int.TryParse(port, out int number) && number >= 1 && number <= 65535;
    }

    byte[] Hash(string pass, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(pass, salt, 100000, HashAlgorithmName.SHA256, 32);
    }

    // we never store the real password, only "salt:hash"
    void SavePassword(string mail, string pass)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        settings.Values["user_" + mail] = Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(Hash(pass, salt));
    }

    bool CheckPassword(string mail, string pass)
    {
        string[] parts = ((string)settings.Values["user_" + mail]).Split(':');
        byte[] salt = Convert.FromBase64String(parts[0]);
        return Convert.ToBase64String(Hash(pass, salt)) == parts[1];
    }

    // returns "" if the password is fine, otherwise the problem
    string PasswordProblem(string pass, string confirm)
    {
        if (pass.Length < 8) return "Password must be at least 8 characters.";
        if (!pass.Any(char.IsDigit)) return "Password must contain at least one number.";
        if (pass != confirm) return "The two passwords do not match.";
        return "";
    }

    // ---------------- login ----------------

    void SignIn_Click(object sender, RoutedEventArgs e) { SignIn(); }
    void SignOut_Click(object sender, RoutedEventArgs e) { ShowScreen("login"); }

    void PassBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter) SignIn();
    }

    void SignIn()
    {
        ClearMessages();
        string mail = EmailBox.Text.Trim().ToLower();
        string pass = PassBox.Password;

        if (DateTime.Now < lockedUntil)
        {
            int seconds = (int)(lockedUntil - DateTime.Now).TotalSeconds + 1;
            ShowMessage(LoginError, "Too many wrong attempts. Try again in " + seconds + " seconds.");
            return;
        }
        if (!IsValidEmail(mail)) { ShowMessage(EmailError, "Please use your " + AllowedDomain + " email address."); return; }
        if (pass == "") { ShowMessage(PassError, "Please enter your password."); return; }
        if (!settings.Values.ContainsKey("user_" + mail)) { ShowMessage(EmailError, "No account found with this email. Create one below."); return; }

        if (!CheckPassword(mail, pass))
        {
            failedAttempts++;
            if (failedAttempts >= 5)
            {
                lockedUntil = DateTime.Now.AddSeconds(30);
                failedAttempts = 0;
                ShowMessage(LoginError, "Too many wrong attempts. Try again in 30 seconds.");
            }
            else ShowMessage(PassError, "Incorrect password. " + (5 - failedAttempts) + " attempts left.");
            return;
        }

        // login is correct
        failedAttempts = 0;
        if (RememberCheck.IsChecked == true) settings.Values["rememberedEmail"] = mail;
        else settings.Values.Remove("rememberedEmail");

        // use the part before @ as the name
        string name = char.ToUpper(mail[0]) + mail.Split('@')[0].Substring(1);
        ProfileName.Text = name;
        ProfilePic.DisplayName = name;

        int hour = DateTime.Now.Hour;
        string greeting = "Good Evening";
        if (hour < 12) greeting = "Good Morning";
        else if (hour < 17) greeting = "Good Afternoon";
        GreetingText.Text = greeting + ", " + name + "!";

        ShowScreen("dash");
    }

    async void SignUp_Click(object sender, RoutedEventArgs e)
    {
        var email = new TextBox { Header = "Email (" + AllowedDomain + ")" };
        var pass = new PasswordBox { Header = "Password (min 8 characters, with a number)" };
        var confirm = new PasswordBox { Header = "Confirm password" };
        var error = MakeError();
        var dialog = MakeDialog("Create account", new StackPanel { Spacing = 10, Children = { email, pass, confirm, error } }, "Create");

        // keep showing the dialog until everything is correct or the user cancels
        while (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            string mail = email.Text.Trim().ToLower();
            string problem = PasswordProblem(pass.Password, confirm.Password);
            if (!IsValidEmail(mail)) problem = "Please use your " + AllowedDomain + " email address.";
            else if (settings.Values.ContainsKey("user_" + mail)) problem = "An account with this email already exists.";

            if (problem != "") { ShowMessage(error, problem); continue; }

            SavePassword(mail, pass.Password);
            ClearMessages();
            EmailBox.Text = mail;
            ShowMessage(LoginInfo, "Account created. You can sign in now.");
            return;
        }
    }

    async void ForgotPassword_Click(object sender, RoutedEventArgs e)
    {
        // there is no email server in this project, so the code is shown on screen
        string code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        var email = new TextBox { Header = "Registered email", Text = EmailBox.Text.Trim() };
        var info = new TextBlock { Text = "Demo mode: a real app would email this code. Your code is " + code, TextWrapping = TextWrapping.Wrap };
        var codeBox = new TextBox { Header = "6-digit code" };
        var newPass = new PasswordBox { Header = "New password (min 8 characters, with a number)" };
        var confirm = new PasswordBox { Header = "Confirm new password" };
        var error = MakeError();
        var dialog = MakeDialog("Reset password", new StackPanel { Spacing = 10, Children = { email, info, codeBox, newPass, confirm, error } }, "Reset");

        while (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            string mail = email.Text.Trim().ToLower();
            string problem = PasswordProblem(newPass.Password, confirm.Password);
            if (!settings.Values.ContainsKey("user_" + mail)) problem = "No account found with this email.";
            else if (codeBox.Text.Trim() != code) problem = "The code is not correct.";

            if (problem != "") { ShowMessage(error, problem); continue; }

            SavePassword(mail, newPass.Password);
            failedAttempts = 0;
            lockedUntil = DateTime.MinValue;
            ClearMessages();
            EmailBox.Text = mail;
            ShowMessage(LoginInfo, "Password changed. Please sign in with your new password.");
            return;
        }
    }

    // ---------------- dashboard ----------------

    async void Start_Click(object sender, RoutedEventArgs e)
    {
        // CONTROLLER TEAM: the real meeting ID and port come from the controller.
        // These two lines are only placeholders until it is connected.
        string id = "123-456-789";
        string port = "5000";

        StartMeeting("Instant Meeting", id, port);
        await ShowMeetingInfo(); // so the host can share the ID and port
    }

    async void Join_Click(object sender, RoutedEventArgs e)
    {
        var idBox = new TextBox { Header = "Meeting ID", PlaceholderText = "e.g. 123-456-789" };
        var portBox = new TextBox { Header = "Port", PlaceholderText = "e.g. 5000" };
        var error = MakeError();
        var dialog = MakeDialog("Join meeting", new StackPanel { Spacing = 10, Children = { idBox, portBox, error } }, "Join");

        // keep showing the dialog until the details look right or the user cancels
        while (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            string id = idBox.Text.Trim();
            string port = portBox.Text.Trim();

            string problem = "";
            if (id == "") problem = "Please enter the meeting ID.";
            else if (!IsValidPort(port)) problem = "Port must be a number between 1 and 65535.";

            if (problem != "") { ShowMessage(error, problem); continue; }

            // CONTROLLER TEAM: connect to the meeting using this id and port.
            StartMeeting("Meeting " + id, id, port);
            return;
        }
    }

    async void Schedule_Click(object sender, RoutedEventArgs e)
    {
        var titleBox = new TextBox { Header = "Title" };
        var datePicker = new CalendarDatePicker { Header = "Date" };
        var panel = new StackPanel { Spacing = 12, Children = { titleBox, datePicker } };

        if (await MakeDialog("Schedule meeting", panel, "Schedule").ShowAsync() != ContentDialogResult.Primary) return;

        string title = titleBox.Text.Trim() == "" ? "New Meeting" : titleBox.Text.Trim();
        string date = datePicker.Date == null ? "Today" : datePicker.Date.Value.ToString("ddd, dd MMM");
        records.Insert(0, title + " - " + date + " - Scheduled");
    }

    // ---------------- meeting room ----------------

    void StartMeeting(string title, string id, string port)
    {
        meetingTitle = title;
        meetingId = id;
        meetingPort = port;
        MeetingTitleText.Text = title;
        ChatBtn.IsChecked = false;
        ShowChat(false);
        ShowPanel("video");
        ShowScreen("meeting");
    }

    // Video, Share and Whiteboard use the same area, so only one is shown
    void ShowPanel(string which)
    {
        VideoPanel.Visibility = Vis(which == "video");
        SharePanel.Visibility = Vis(which == "share");
        BoardPanel.Visibility = Vis(which == "board");
        VideoBtn.IsChecked = which == "video";
        ShareBtn.IsChecked = which == "share";
        BoardBtn.IsChecked = which == "board";
    }

    void VideoBtn_Click(object sender, RoutedEventArgs e) { ShowPanel("video"); }
    void ShareBtn_Click(object sender, RoutedEventArgs e) { ShowPanel("share"); }
    void BoardBtn_Click(object sender, RoutedEventArgs e) { ShowPanel("board"); }
    void ChatBtn_Click(object sender, RoutedEventArgs e) { ShowChat(ChatBtn.IsChecked == true); }

    // the chat panel and its drag handle are shown or hidden together
    void ShowChat(bool show)
    {
        ChatPanel.Visibility = Vis(show);
        ChatResizer.Visibility = Vis(show);
    }

    // ---------------- resizing the chat ----------------

    void ChatResizer_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        resizingChat = true;
        ChatResizer.CapturePointer(e.Pointer);
    }

    void ChatResizer_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!resizingChat) return;

        // the chat sits against the right edge (16 px margin), so the width is
        // the distance from the cursor to that edge
        double cursorX = e.GetCurrentPoint(MeetingView).Position.X;
        double width = MeetingView.ActualWidth - 16 - cursorX;

        // not too small, and never more than half of the window
        double maxWidth = MeetingView.ActualWidth / 2;
        ChatPanel.Width = Math.Clamp(width, 250, maxWidth);
    }

    void ChatResizer_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        resizingChat = false;
        ChatResizer.ReleasePointerCapture(e.Pointer);
    }
    async void InfoBtn_Click(object sender, RoutedEventArgs e) { await ShowMeetingInfo(); }

    // shows the meeting ID and port so they can be copied and shared
    async Task ShowMeetingInfo()
    {
        var details = new TextBlock { Text = "Meeting ID: " + meetingId + "\nPort: " + meetingPort, FontSize = 18, IsTextSelectionEnabled = true };
        var note = new TextBlock { Text = "Share these with the people you want to invite.", TextWrapping = TextWrapping.Wrap };
        var dialog = MakeDialog("Meeting details", new StackPanel { Spacing = 10, Children = { note, details } }, "Copy");
        dialog.CloseButtonText = "Close";

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var data = new DataPackage();
        data.SetText("Meeting ID: " + meetingId + ", Port: " + meetingPort);
        Clipboard.SetContent(data);
    }

    async void End_Click(object sender, RoutedEventArgs e)
    {
        var result = await MakeDialog("End meeting", "Are you sure you want to end this meeting?", "End Meeting").ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        records.Insert(0, meetingTitle + " - " + DateTime.Now.ToString("ddd, dd MMM") + " - Just now");
        ShowScreen("dash");
    }
}

// A thin strip used to resize the chat panel.
public sealed class ResizeHandle : ContentControl
{
    public ResizeHandle()
    {
        ProtectedCursor =
            InputSystemCursor.Create(
                InputSystemCursorShape.SizeWestEast);
    }
}