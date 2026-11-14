using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Cloud.Persistence;
using FileSynchroniser.Models;
using FileSynchroniser.Views;
using Incidents;
using Incidents.Views;
using LocationTracker.Services;
using LocationTracker.Views;
using Controller.Models;
using Controller.ViewModels;
using Controller;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinRT.Interop;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using UX.Models;
using Windows.Storage.Pickers;
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
    private string _userEmail = string.Empty;
    private string _picture = string.Empty;
    private string? _pickedPicture;
    private string _meetingId = string.Empty;
    private string _meetingPort = string.Empty;
    private bool _inMeeting;
    private bool _isHost;
    private bool _resizingChat;
    private readonly MeetingSession _session = new();
    private readonly ControllerMeetingService _meetingService = new();

    private string Role => _isHost ? HostRole : ParticipantRole;

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

        _session.ParticipantCountChanged += count => {
            DispatcherQueue.TryEnqueue(() => SetParticipants(count));
        };
        _session.MeetingEnded += () => {
            DispatcherQueue.TryEnqueue(() => {
                if (_inMeeting)
                {
                    _inMeeting = false;
                    ShowProfile();
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
        UserPanel.Visibility = Vis(screen != "login");
        MeetingInfoText.Visibility = Vis(_inMeeting);
    }

    private ContentDialog MakeDialog(string title, object content, string okText)
    {
        return new ContentDialog { Title = title, Content = content, PrimaryButtonText = okText, CloseButtonText = "Cancel", XamlRoot = Root.XamlRoot };
    }

    // shows a picture (file path or web address) in a PersonPicture
    private static void SetPicture(PersonPicture pic, string? source)
    {
        pic.ProfilePicture = string.IsNullOrEmpty(source) ? null : new BitmapImage(new Uri(source));
    }

    // lets the person choose a picture file and returns its path (null if they cancel)
    private async Task<string?> PickPictureAsync()
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    // shows the name, picture and greeting of the signed-in person
    private void ShowProfile()
    {
        ProfileName.Text = _inMeeting ? _userId + " (" + Role + ")" : _userId;
        ProfilePic.DisplayName = _userId;
        SetPicture(ProfilePic, _picture);

        // CONTROLLER TEAM: store current user identity into session
        _session.CurrentUserId = _userId;
        _session.CurrentUserEmail = _userEmail;

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
    }

    private async void PickLoginPicture_Click(object sender, RoutedEventArgs e)
    {
        string? path = await PickPictureAsync();
        if (path != null)
        {
            _pickedPicture = path;
            SetPicture(LoginPic, path);
        }
    }

    // opens the browser so the person can log in with their Google account
    private async void SignIn_Click(object sender, RoutedEventArgs e)
    {
        SignInBtn.IsEnabled = false;
        SignInBtn.Content = "Waiting for Google...";
        LoginError.Visibility = Visibility.Collapsed;

        try
        {
            string typedId = UserIdBox.Text.Trim();
            GoogleUser user = await GoogleAuth.SignInAsync();
            Activate();

            string mail = user.Email.Trim().ToLowerInvariant();
            if (!mail.EndsWith(AllowedDomain, StringComparison.Ordinal) || mail == AllowedDomain)
            {
                LoginError.Text = "Please sign in with your " + AllowedDomain + " Google account.";
                LoginError.Visibility = Visibility.Visible;
                return;
            }

            SavedProfile? saved = ProfileStore.Load(mail);
            _userEmail = mail;
            _userId = typedId.Length > 0 ? typedId : (saved?.UserId ?? user.Name);
            string savedPicture = saved?.PicturePath ?? string.Empty;
            if (_pickedPicture != null)
            {
                savedPicture = ProfileStore.CopyPicture(_pickedPicture);
            }

            if (!File.Exists(savedPicture))
            {
                savedPicture = string.Empty;
            }

            _picture = savedPicture.Length > 0 ? savedPicture : user.Picture;
            ProfileStore.Save(mail, _userId, savedPicture);

            // CONTROLLER TEAM: store current user identity into session
            _session.CurrentUserId = _userId;
            _session.CurrentUserEmail = mail;

            ShowProfile();
            ShowScreen("dash");
        }
        catch (Exception ex)
        {
            LoginError.Text = "Google sign-in failed: " + ex.Message;
            LoginError.Visibility = Visibility.Visible;
        }
        finally
        {
            SignInBtn.IsEnabled = true;
            SignInBtn.Content = "Sign in with Google";
        }
    }

    private async void EditProfile_Click(object sender, RoutedEventArgs e)
    {
        string? newPicture = null;
        var pic = new PersonPicture { Width = 80, Height = 80 };
        SetPicture(pic, _picture);
        var changePic = new Button { Content = "Change picture", HorizontalAlignment = HorizontalAlignment.Center };
        changePic.Click += async (_, _) => {
            string? path = await PickPictureAsync();
            if (path != null)
            {
                newPicture = path;
                SetPicture(pic, path);
            }
        };
        var idBox = new TextBox { Header = "User ID", Text = _userId };
        var error = new TextBlock { Foreground = new SolidColorBrush(Colors.Red) };
        ContentDialog dialog = MakeDialog("Edit profile", new StackPanel { Spacing = 10, Children = { pic, changePic, idBox, error } }, "Save");
        while (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            string id = idBox.Text.Trim();
            if (id.Length == 0)
            {
                error.Text = "User ID cannot be empty.";
                continue;
            }

            if (newPicture != null)
            {
                _picture = ProfileStore.CopyPicture(newPicture);
            }

            if (id != _userId)
            {
                // the incident pages were created with the old user ID, so rebuild them on next use
                _incidentContextTask = null;
            }

            _userId = id;
            _session.CurrentUserId = _userId;
            ShowProfile();
            ProfileStore.Save(_userEmail, _userId, _picture.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? string.Empty : _picture);
            return;
        }
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
        _userId = string.Empty;
        _userEmail = string.Empty;
        _picture = string.Empty;
        _pickedPicture = null;
        UserIdBox.Text = string.Empty;
        LoginPic.ProfilePicture = null;
        ShowScreen("login");
    }

    private void Cloud_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen("cloud");
    }

    private void CloudBack_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen(_inMeeting ? "meeting" : "dash");
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
        _meetingId = id;
        _meetingPort = port;

        EndBtn.Content = host ? "End Meeting" : "Leave Meeting";
        ShowProfile();

        // CONTROLLER TEAM: call SetParticipants() with the real number
        SetParticipants(_session.ParticipantCount);

        ChatBtn.IsChecked = false;
        ShowChat(false);
        ShowPanel("home");
        ShowScreen("meeting");
    }

    public void SetParticipants(int count)
    {
        MeetingInfoText.Text = "Role: " + Role + "   ID: " + _meetingId + "   Port: " + _meetingPort + "   Participants: " + count;
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
        FileSyncFrame.Navigate(typeof(FileSyncPage), _meetingService.ActiveController);
        ShowPanel("filesync");
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
        ShowProfile();
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

    private void IncidentsBack_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen(_inMeeting ? "meeting" : "dash");
    }

    private void LocationBack_Click(object sender, RoutedEventArgs e)
    {
        ShowScreen(_inMeeting ? "meeting" : "dash");
    }
}

public sealed class ResizeHandle : ContentControl
{
    public ResizeHandle()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }
}
