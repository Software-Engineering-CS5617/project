using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Chat;
using Cloud.Persistence;
using FileSynchroniser.Models;
using FileSynchroniser.Views;
using Incidents;
using Incidents.Views;
using LocationTracker.Services;
using LocationTracker.Views;
using Controller;
using Controller.Models;
using Controller.ViewModels;
using UI.Services;
using UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinRT.Interop;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;


namespace UX;

public sealed partial class MainWindow : Window
{
    private const string HostRole = "Host";
    private const string ParticipantRole = "Participant";
    private const string AllowedDomain = "@smail.iitpkd.ac.in";
    private readonly ObservableCollection<string> _records = new() { "Weekly Review - Mon, 28 Sep - 45 min", "Patrol Planning - Fri, 25 Sep - 30 min", "Case Briefing - Wed, 23 Sep - 1 hr 10 min" };
    private Task<IncidentPageContext>? _incidentContextTask;
    private string _userId = string.Empty;
    private string _meetingId = string.Empty;
    private string _meetingPort = string.Empty;
    private string _role = ParticipantRole;
    private bool _inMeeting;
    private bool _isHost;
    private bool _resizingChat;
    private readonly MeetingSession _session = new();
    private readonly ControllerMeetingService _meetingService = new();
    private ChatServer? _chatServer;
    private ChatClient? _chatClient;
    private ChatClientAdapter? _chatAdapter;
    private ChatPanelViewModel? _chatViewModel;

    public MainWindow()
    {
        InitializeComponent();
        Title = "SecureMeet";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
        RecordsList.ItemsSource = _records;

        IncidentsFrame.Navigated += IncidentsFrame_Navigated;

        // CONTROLLER TEAM: initialize controller service and bind MeetingView
        _meetingService.Initialize(_session);
        ControllerMeetingView.ViewModel = new MeetingViewModel(_session);

        // SCREEN SHARING TEAM: runs ScreenShare for every meeting, shown in SharePanel
        _ = new ScreenShare.ScreenShareMeetingIntegration(_meetingService, _session, ScreenShareView);
        _session.ParticipantCountChanged += count => {
            DispatcherQueue.TryEnqueue(() => SetParticipants(count));
        };
        _session.MeetingEnded += () => {
            DispatcherQueue.TryEnqueue(() => {
                if (_inMeeting)
                {
                    _inMeeting = false;
                    ProfileName.Text = _userId;
                    ShowScreen("dash");
                }
            });
        };
        _session.MeetingEnding += DisposeChatSession;
        _session.ParticipantsChanged += participants => {
            if (_inMeeting && !_isHost && _chatViewModel is null && participants.Any(participant => participant.IsHost))
                DispatcherQueue.TryEnqueue(() => TryInitializeChatSession(false));
        };
        Closed += (_, _) => DisposeChatSession();

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
        _incidentContextTask = null;
        DisposeChatSession();
        ShowScreen("login");
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        // CONTROLLER TEAM: the real meeting ID and port come from the controller.
        (string? id, string? port) = _meetingService.CreateMeeting();
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
        TryInitializeChatSession(host);
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
        FileSyncView.Visibility = Vis(which == "filesync");

        HomeBtn.IsChecked = which == "home";
        ShareBtn.IsChecked = which == "share";
        BoardBtn.IsChecked = which == "board";
        FileSyncBtn.IsChecked = which == "filesync";
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

    private void FileSyncBtn_Click(object sender, RoutedEventArgs e)
    {
        FileSyncFrame.Navigate(typeof(FileSyncPage));
        ShowPanel("filesync");
    }

    private void ChatBtn_Click(object sender, RoutedEventArgs e)
    {
        if (ChatBtn.IsChecked == true)
            TryInitializeChatSession(_isHost);

        ShowChat(ChatBtn.IsChecked == true);
    }

    private void ShowChat(bool show)
    {
        ChatPanel.Visibility = Vis(show);
        ChatResizer.Visibility = Vis(show);
    }

    private void TryInitializeChatSession(bool isHost)
    {
        if (_chatViewModel is not null)
            return;

        IControllerService? controller = _meetingService.ActiveController;
        if (controller?.Communicator is null || controller.CurrentUser is null)
            return;

        try
        {
            string username = controller.CurrentUser.Username;
            string hostName = isHost
                ? username
                : controller.GetActiveUsers().FirstOrDefault(user => user.IsHost)?.Username
                  ?? _session.ActiveParticipants.FirstOrDefault(participant => participant.IsHost)?.Username
                  ?? string.Empty;

            // A participant needs the host's username before the local room is valid.
            if (string.IsNullOrWhiteSpace(hostName))
                return;

            var room = new Chatroom($"{_meetingId}:{_meetingPort}", hostName);

            if (isHost)
            {
                _chatServer = new ChatServer(room, controller);
                _chatAdapter = new ChatClientAdapter(_chatServer, controller, DispatcherQueue);
            }
            else
            {
                string hostIp = _meetingId.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    ? "127.0.0.1"
                    : _meetingId;

                if (!IPAddress.TryParse(hostIp, out IPAddress? address)
                    || address is null
                    || !int.TryParse(_meetingPort, out int port))
                    return;

                _chatClient = new ChatClient(username, room, controller.Communicator, new IPEndPoint(address, port));
                _chatAdapter = new ChatClientAdapter(_chatClient, DispatcherQueue);
            }

            _chatViewModel = new ChatPanelViewModel(_chatAdapter);
            MeetingChatPanel.ViewModel = _chatViewModel;
            _chatViewModel.Initialize();
        }
        catch (Exception exception)
        {
            DisposeChatSession();
            System.Diagnostics.Trace.WriteLine($"[Chat] Could not start the chat session: {exception}");
        }
    }

    private void DisposeChatSession()
    {
        MeetingChatPanel.ViewModel = null;
        _chatViewModel?.Dispose();
        _chatAdapter?.Dispose();
        _chatClient?.Dispose();
        _chatServer?.Dispose();
        _chatViewModel = null;
        _chatAdapter = null;
        _chatClient = null;
        _chatServer = null;
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
        _incidentContextTask ??= InitializeIncidentContextAsync();
        return await _incidentContextTask;
    }

    private async Task<IncidentPageContext> InitializeIncidentContextAsync()
    {
        IStorage storage = await StorageFactory.CreateAsync(ModuleType.IncidentManagement);
        IFileOperations fileSynchronizer = new Synchroniser();
        IIncidentController controller = await IncidentController.CreateAsync(storage, fileSynchronizer);

        IntPtr windowHandle = WindowNative.GetWindowHandle(this);
        return new IncidentPageContext(controller, _userId, windowHandle);
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
            var map = new LocationMapView(new MapService(context.IncidentController, geocoding));
            map.IncidentSelected += LocationIncidentSelected;
            map.LocationSelected += LocationSelectedOnMap;
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

    /// <summary>
    /// Handles a location selection on the map by navigating to the create incident page.
    /// </summary>
    private async void LocationSelectedOnMap(IncidentLocation location)
    {
        try
        {
            IncidentPageContext context = await GetIncidentContextAsync();
            IncidentsFrame.Navigate(
                typeof(CreateIncidentPage),
                new IncidentCreatePageWithLocation(
                    context,
                    location.Latitude,
                    location.Longitude,
                    location.Address));
            LocationHost.Content = null;
            ShowScreen("incidents");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Unable to open create incident", ex.Message);
        }
    }

    /// <summary>
    /// Adds the location tracker mini map whenever the create incident page is opened.
    /// </summary>
    private void IncidentsFrame_Navigated(object sender, NavigationEventArgs e)
    {
        if (e.Content is CreateIncidentPage page)
        {
            MiniMapView.AttachTo(page);
        }
    }


    private async Task ShowErrorAsync(string title, string message)
    {
        await new ContentDialog {
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
}

public sealed class ResizeHandle : ContentControl
{
    public ResizeHandle()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }
}
