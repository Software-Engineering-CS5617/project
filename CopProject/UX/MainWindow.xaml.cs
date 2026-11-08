using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using Cloud.Persistence;
using FileSynchroniser.Models;
using Incident_module;
using Incident_module.Implementations;
using Incident_module.Views;
using LocationTracker.Services;
using LocationTracker.Views;
using Controller.Models;
using Controller.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinRT.Interop;
using Microsoft.UI.Xaml.Media;

namespace UX;
public sealed partial class MainWindow : Window
{
    private const string HostRole = "Host";
    private const string ParticipantRole = "Participant";
    private const string AllowedDomain = "@smail.iitpkd.ac.in";
    private readonly ObservableCollection<string> _records = new() { "Weekly Review - Mon, 28 Sep - 45 min", "Patrol Planning - Fri, 25 Sep - 30 min", "Case Briefing - Wed, 23 Sep - 1 hr 10 min" };
    private Task<IncidentPageContext>? incidentContextTask;
    private string _userId = string.Empty;
    private string _meetingId = string.Empty;
    private string _meetingPort = string.Empty;
    private string _role = ParticipantRole;
    private bool _inMeeting;
    private bool _isHost;
    private bool _resizingChat;
    private readonly MeetingSession _session = new();      
    private readonly ControllerMeetingService _meetingService = new();
    public MainWindow()
    {
        InitializeComponent();
        Title = "SecureMeet";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
        RecordsList.ItemsSource = _records;

        // CONTROLLER TEAM: initialize controller service and bind MeetingView
        _meetingService.Initialize(_session);
        ControllerMeetingView.ViewModel = new MeetingViewModel(_session);

        _session.ParticipantCountChanged += count =>
        {
            DispatcherQueue.TryEnqueue(() => SetParticipants(count));
        };
        _session.MeetingEnded += () =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_inMeeting)
                {
                    _inMeeting = false;
                    ProfileName.Text = _userId;
                    ShowScreen("dash");
                }
            });
        };

        ShowScreen("login");
    }

    private static Visibility Vis(bool show)
    {
        return show ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowScreen(string screen)
    {
        LoginView.Visibility = Vis(screen == "login");
        DashView.Visibility = Vis(screen == "dash");
        MeetingView.Visibility = Vis(screen == "meeting");
        IncidentsView.Visibility = Vis(screen == "incidents");
        LocationView.Visibility = Vis(screen == "location");
        CloudView.Visibility = Vis(screen == "cloud");
        FileSyncView.Visibility = Vis(screen == "filesync");
        UserPanel.Visibility = Vis(screen != "login");
        MeetingInfoText.Visibility = Vis(_inMeeting);
    }

    private ContentDialog MakeDialog(string title, object content, string okText)
    {
        return new ContentDialog { Title = title, Content = content, PrimaryButtonText = okText, CloseButtonText = "Cancel", XamlRoot = Root.XamlRoot };
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        string id = UserIdBox.Text.Trim();
        string mail = EmailBox.Text.Trim().ToLowerInvariant();

        if (id.Length == 0)
        {
            LoginError.Text = "Please enter your user ID.";
            LoginError.Visibility = Visibility.Visible;
            return;
        }

        if (!mail.EndsWith(AllowedDomain, StringComparison.Ordinal) || mail == AllowedDomain)
        {
            LoginError.Text = "Please use your " + AllowedDomain + " email address.";
            LoginError.Visibility = Visibility.Visible;
            return;
        }

        _userId = id;
        LoginError.Visibility = Visibility.Collapsed;
        ProfileName.Text = _userId;
        ProfilePic.DisplayName = _userId;

        // CONTROLLER TEAM: store current user identity into session
        _session.CurrentUserId = _userId;
        _session.CurrentUserEmail = mail;

        int hour = DateTime.Now.Hour;
        string greeting = "Good Evening";
        if (hour < 12)
        {
            greeting = "Good Morning";
        }
        else if (hour < 17)
        {
            greeting = "Good Afternoon";
        }

        GreetingText.Text = greeting + ", " + _userId + "!";

        ShowScreen("dash");
    }

    private void SignOut_Click(object sender, RoutedEventArgs e)
    {
        if (_inMeeting)
        {
            _session.EndMeeting();
        }
        _inMeeting = false;
        LocationHost.Content = null;
        incidentContextTask = null;
        ShowScreen("login");
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        // CONTROLLER TEAM: the real meeting ID and port come from the controller.
        var (id, port) = _meetingService.CreateMeeting();
        _session.StartMeeting(new MeetingInfo(id, port, IsHost: true));

        StartMeeting(id, port, true);
    }

    private async void Join_Click(object sender, RoutedEventArgs e)
    {
        var idBox = new TextBox { Header = "Meeting ID", PlaceholderText = "e.g. 192.168.1.5" };
        var portBox = new TextBox { Header = "Port", PlaceholderText = "e.g. 5000" };
        var error = new TextBlock { Foreground = new SolidColorBrush(Colors.Red) };
        ContentDialog dialog = MakeDialog("Join meeting", new StackPanel { Spacing = 10, Children = { idBox, portBox, error } }, "Join");
        while (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            string id = idBox.Text.Trim();
            string port = portBox.Text.Trim();

            if (id.Length == 0)
            {
                error.Text = "Please enter the meeting ID.";
                continue;
            }

            if (!int.TryParse(port, out int number) || number < 1 || number > 65535)
            {
                error.Text = "Port must be a number between 1 and 65535.";
                continue;
            }

            // CONTROLLER TEAM: connect to the meeting using this id and port.
            bool joined = _meetingService.JoinMeeting(id, number);
            if (!joined)
            {
                error.Text = "Could not connect to meeting at " + id + ":" + port;
                continue;
            }

            _session.StartMeeting(new MeetingInfo(id, port, IsHost: false));
            StartMeeting(id, port, false);
            return;
        }
    }

    private void StartMeeting(string id, string port, bool host)
    {
        _inMeeting = true;
        _isHost = host;
        _role = host ? HostRole : ParticipantRole;
        _meetingId = id;
        _meetingPort = port;

        EndBtn.Content = host ? "End Meeting" : "Leave Meeting";
        ProfileName.Text = _userId + " (" + _role + ")";

        // CONTROLLER TEAM: call SetParticipants() with the real number
        SetParticipants(_session.ParticipantCount);

        ChatBtn.IsChecked = false;
        ShowChat(false);
        ShowPanel("home");
        ShowScreen("meeting");
    }

    public void SetParticipants(int count)
    {
        MeetingInfoText.Text = "Role: " + _role + "   ID: " + _meetingId + "   Port: " + _meetingPort + "   Participants: " + count;
    }

    private void ShowPanel(string which)
    {
        HomePanel.Visibility = Vis(which == "home");
        SharePanel.Visibility = Vis(which == "share");
        BoardPanel.Visibility = Vis(which == "board");
        HomeBtn.IsChecked = which == "home";
        ShareBtn.IsChecked = which == "share";
        BoardBtn.IsChecked = which == "board";
    }

    private void HomeBtn_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel("home");
    }

    private void ShareBtn_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel("share");
    }

    private void BoardBtn_Click(object sender, RoutedEventArgs e)
    {
        ShowPanel("board");
    }

    private void ChatBtn_Click(object sender, RoutedEventArgs e)
    {
        ShowChat(ChatBtn.IsChecked == true);
    }

    private void ShowChat(bool show)
    {
        ChatPanel.Visibility = Vis(show);
        ChatResizer.Visibility = Vis(show);
    }

    private async void End_Click(object sender, RoutedEventArgs e)
    {
        string text = _isHost
            ? "Are you sure you want to end this meeting?"
            : "Are you sure you want to leave this meeting?";
        string buttonText = EndBtn.Content.ToString() ?? string.Empty;

        ContentDialogResult result = await MakeDialog(buttonText, text, buttonText).ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        // CONTROLLER TEAM: end the meeting (Host) or disconnect (Participant) here.
        _session.EndMeeting();

        _records.Insert(0, "Meeting " + _meetingId + " - " + DateTime.Now.ToString("ddd, dd MMM") + " - Just now");
        _inMeeting = false;
        ProfileName.Text = _userId;
        ShowScreen("dash");
    }

    private void ChatResizer_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _resizingChat = true;
        ChatResizer.CapturePointer(e.Pointer);
    }

    private void ChatResizer_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_resizingChat)
        {
            return;
        }
        double cursorX = e.GetCurrentPoint(MeetingView).Position.X;
        double width = MeetingView.ActualWidth - 16 - cursorX;
        ChatPanel.Width = Math.Clamp(width, 250, MeetingView.ActualWidth / 2);
    }

    private void ChatResizer_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _resizingChat = false;
        ChatResizer.ReleasePointerCapture(e.Pointer);
    }

    private async Task<IncidentPageContext> GetIncidentContextAsync()
    {
        incidentContextTask ??= InitializeIncidentContextAsync();
        return await incidentContextTask;
    }

    private async Task<IncidentPageContext> InitializeIncidentContextAsync()
    {
        IStorage storage = await StorageFactory.CreateAsync(ModuleType.IncidentManagement);
        IFileOperations fileSynchronizer = new Synchroniser();
        IIncidentController controller = await IncidentController.CreateAsync(storage, fileSynchronizer);

        Guid currentUserId = Guid.TryParse(_userId, out Guid parsedUserId)
            ? parsedUserId
            : Guid.NewGuid();

        IntPtr windowHandle = WindowNative.GetWindowHandle(this);
        return new IncidentPageContext(controller, currentUserId, windowHandle);
    }

    private async void Incidents_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            IncidentPageContext context = await GetIncidentContextAsync();
            IncidentsFrame.Navigate(typeof(AllIncidentsPage), context);
            ShowScreen("incidents");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Unable to load incidents", ex.Message);
        }
    }

    private async void Location_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            IncidentPageContext context = await GetIncidentContextAsync();
            var geocoding = new NominatimGeocodingService(new HttpClient());
            var map = new LocationMapView();
            map.IncidentSelected += LocationIncidentSelected;
            map.Initialize(new MapService(context.IncidentController, geocoding));
            LocationHost.Content = map;
            ShowScreen("location");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Unable to load location", ex.Message);
        }
    }

    private async void LocationIncidentSelected(Guid incidentId)
    {
        try
        {
            IncidentPageContext context = await GetIncidentContextAsync();
            IncidentsFrame.Navigate(
                typeof(IncidentDetailsPage),
                new IncidentDetailsPageParameters(context, incidentId));
            LocationHost.Content = null;
            ShowScreen("incidents");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Unable to load incident", ex.Message);
        }
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        await new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Root.XamlRoot
        }.ShowAsync();
    }

    private void Cloud_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen("cloud");
    }

    private void FileSync_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen("filesync");
    }

    private void IncidentsBack_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen(_inMeeting ? "meeting" : "dash");
    }

    private void LocationBack_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen(_inMeeting ? "meeting" : "dash");
    }

    private void CloudBack_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen("dash");
    }

    private void FileSyncBack_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen("dash");
    }
}

public sealed class ResizeHandle : ContentControl
{
    public ResizeHandle()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }
}