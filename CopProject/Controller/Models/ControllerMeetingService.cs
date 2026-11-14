using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Controller;
using Controller.ViewModels;
using Utils;

namespace Controller.Models;

public sealed class ControllerMeetingService
{
    private MeetingSession? _session;
    private IControllerService? _activeController;
    private bool _isHost;
    private readonly SynchronizationContext? _uiThread = SynchronizationContext.Current;
    public IControllerService? ActiveController => _activeController;
    private readonly List<IUserNotificationHandler> _registeredHandlers = new();
    public string? LastErrorMessage { get; private set; }

    public void RegisterNotificationHandler(IUserNotificationHandler handler)
    {
        if (handler != null && !_registeredHandlers.Contains(handler))
        {
            _registeredHandlers.Add(handler);
            _activeController?.RegisterNotificationHandler(handler);
        }
    }

    public void UnregisterNotificationHandler(IUserNotificationHandler handler)
    {
        if (handler != null)
        {
            _registeredHandlers.Remove(handler);
            _activeController?.UnregisterNotificationHandler(handler);
        }
    }

    public void Initialize(MeetingSession session)
    {
        _session = session;
        _session.MeetingStarted += OnMeetingStarted;
        _session.MeetingEnded += OnMeetingEnded;
    }

    public (string Id, string Port) CreateMeeting()
    {
        CleanupController();

        string username = !string.IsNullOrWhiteSpace(_session?.CurrentUserId) ? _session.CurrentUserId : "Host";
        string email = !string.IsNullOrWhiteSpace(_session?.CurrentUserEmail) ? _session.CurrentUserEmail : $"{username}@smail.iitpkd.ac.in";

        string localIp = NetworkUtils.GetLocalIpAddress();
        var hostUser = new ControllerUser(username, email, _session?.CurrentUserProfilePicture, localIp, 0, isHost: true);

        var host = ControllerFactory.CreateHost(hostUser);
        _activeController = host;
        _isHost = true;
        foreach (var handler in _registeredHandlers)
        {
            host.RegisterNotificationHandler(handler);
        }
        bool started = host.StartMeeting();
        if (!started)
        {
            Trace.TraceWarning("HostController failed to start meeting.");
        }

        string id = localIp;
        string port = host.ListenPort.ToString();

        return (id, port);
    }

    public bool JoinMeeting(string hostIp, int hostPort)
    {
        CleanupController();
        _isHost = false;

        string username = !string.IsNullOrWhiteSpace(_session?.CurrentUserId) ? _session.CurrentUserId : "Participant";
        string email = !string.IsNullOrWhiteSpace(_session?.CurrentUserEmail) ? _session.CurrentUserEmail : $"{username}@smail.iitpkd.ac.in";

        var clientUser = new ControllerUser(username, email, _session?.CurrentUserProfilePicture, NetworkUtils.GetLocalIpAddress(), 0, isHost: false);
        var client = ControllerFactory.CreateClient(clientUser);

        string targetHost = hostIp.Trim();
        if (targetHost.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            targetHost = "127.0.0.1";
        }

        bool joined = client.JoinMeeting(targetHost, hostPort, clientUser);
        if (!joined)
        {
            LastErrorMessage = (client as ClientController)?.LastErrorMessage;
            client.Dispose();
            return false;
        }

        _activeController = client;
        foreach (var handler in _registeredHandlers)
        {
            client.RegisterNotificationHandler(handler);
        }
        HookControllerEvents(client);
        UpdateParticipantCount(client.GetActiveUserCount());
        UpdateActiveParticipants();
        return true;
    }

    public void UpdateProfilePicture(byte[]? picture)
    {
        if (_activeController != null)
        {
            if (_activeController.CurrentUser != null)
            {
                _activeController.CurrentUser.ProfilePicture = picture;
            }
            UpdateActiveParticipants();
        }
    }
    private void OnMeetingStarted(MeetingInfo info)
    {
        if (_session == null) return;

        if (info.IsHost)
        {
            if (_activeController != null)
            {
                HookControllerEvents(_activeController);
                UpdateParticipantCount(_activeController.GetActiveUserCount());
                UpdateActiveParticipants();
            }
        }
        else
        {
            if (_activeController != null)
            {
                UpdateParticipantCount(_activeController.GetActiveUserCount());
                UpdateActiveParticipants();
            }
        }
    }

    private void HookControllerEvents(IControllerService controller)
    {
        controller.UserJoined += OnUserJoined;
        controller.UserLeft += OnUserLeft;
        controller.MeetingEnded += OnRemoteMeetingEnded;
        if (controller is HostController host)
        {
            host.SecurityAlertRaised += OnSecurityAlertRaised;
        }
        else if (controller is ClientController client)
        {
            client.SecurityAlertRaised += OnSecurityAlertRaised;
            client.JoinRejected += OnJoinRejected;
        }
    }

    private void UnhookControllerEvents(IControllerService controller)
    {
        controller.UserJoined -= OnUserJoined;
        controller.UserLeft -= OnUserLeft;
        controller.MeetingEnded -= OnRemoteMeetingEnded;
         if (controller is HostController host)
        {
            host.SecurityAlertRaised -= OnSecurityAlertRaised;
        }
        else if (controller is ClientController client)
        {
            client.SecurityAlertRaised -= OnSecurityAlertRaised;
            client.JoinRejected -= OnJoinRejected;
        }
    }

    private void OnUserJoined(object? sender, UserJoinedEventArgs e)
    {
        UpdateParticipantCount(e.NewCount);
        UpdateActiveParticipants();
    }

    private void OnUserLeft(object? sender, UserLeftEventArgs e)
    {
        UpdateParticipantCount(e.NewCount);
        UpdateActiveParticipants();
    }

    private void OnRemoteMeetingEnded(object? sender, MeetingEndedEventArgs e)
    {
        if (_session != null && _session.InMeeting)
        {
            PostToUI(() =>
            {
                if (_session.InMeeting)
                {
                    _session.EndMeeting();
                }
            });
        }
    }

    private void OnMeetingEnded()
    {
        if (_activeController != null)
        {
            if (_isHost)
            {
                _activeController.EndMeeting();
            }
            else
            {
                string username = !string.IsNullOrWhiteSpace(_session?.CurrentUserId) ? _session.CurrentUserId : "Participant";
                string email = !string.IsNullOrWhiteSpace(_session?.CurrentUserEmail) ? _session.CurrentUserEmail : $"{username}@smail.iitpkd.ac.in";
                var clientUser = new ControllerUser(username, email, null, string.Empty, 0, isHost: false);
                _activeController.LeaveMeeting(clientUser);
            }

            CleanupController();
        }
    }

    private void UpdateParticipantCount(int count)
    {
        if (_session != null)
        {
            PostToUI(() => _session.ParticipantCount = count);
        }
    }

    private void UpdateActiveParticipants()
    {
        if (_activeController == null || _session == null) return;

        var users = _activeController.GetActiveUsers();
        var list = users.Select(u => new ParticipantInfo
        {
            Username = u.Username,
            Email = u.Gmail,
            IsHost = u.IsHost,
            ProfilePicture = u.ProfilePicture
        }).ToList();

        PostToUI(() =>
        {
            _session.ActiveParticipants = list;
        });
    }

    private void PostToUI(Action action)
    {
        if (_uiThread != null)
        {
            _uiThread.Post(_ => action(), null);
        }
        else
        {
            action();
        }
    }

    private void CleanupController()
    {
        if (_activeController != null)
        {
            UnhookControllerEvents(_activeController);
            _activeController.Dispose();
            _activeController = null;
        }
    }

    private void OnSecurityAlertRaised(object? sender, string message)
    {
        _session?.RaiseSecurityAlert(message);
    }

    private void OnJoinRejected(object? sender, string reason)
    {
        if (_session != null)
        {
            _session.EndReason = reason;
            PostToUI(() => _session.EndMeeting());
        }
    }
}