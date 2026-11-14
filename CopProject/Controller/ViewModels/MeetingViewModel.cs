using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Controller.Models;

namespace Controller.ViewModels;

public sealed class MeetingViewModel : ObservableObject
{
    readonly MeetingSession session;
    readonly DispatcherTimer timer;
    private DateTime? _meetingStartedTime;
    private readonly HashSet<string> _activeUsernames = new(StringComparer.OrdinalIgnoreCase);

    public MeetingViewModel(MeetingSession session)
    {
        this.session = session;

        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) =>
        {
            DateTime start = session.MeetingStartTime ?? _meetingStartedTime ?? DateTime.Now;
            var elapsed = DateTime.Now - start;
            DurationText = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
            OnPropertyChanged(nameof(DurationText));
            ChartUpdated?.Invoke();
        };

        session.MeetingStarted += info =>
        {
            _activeUsernames.Clear();
            _meetingStartedTime = session.MeetingStartTime ?? DateTime.Now;
            string user = !string.IsNullOrWhiteSpace(session.CurrentUserId) ? session.CurrentUserId : "Host";
            AuditLogs.Insert(0, new SecurityAuditEvent { User = user, Activity = "Meeting created & started", Status = "Success" });
            AuditLogs.Insert(0, new SecurityAuditEvent { User = user, Activity = "User logged in", Status = "Success" });
            _activeUsernames.Add(user);

            GraphPoints.Clear();
            GraphPoints.Add(new ParticipantDataPoint { Seconds = 0, TimeLabel = "00:00", Count = Math.Max(1, session.ParticipantCount) });
            timer.Start();
            Refresh();
            ChartUpdated?.Invoke();
        };

        session.MeetingEnded += () =>
        {
            string user = !string.IsNullOrWhiteSpace(session.CurrentUserId) ? session.CurrentUserId : "Host";
            AuditLogs.Insert(0, new SecurityAuditEvent { User = user, Activity = "Meeting ended", Status = "Info" });
            timer.Stop();
            _meetingStartedTime = null;
            _activeUsernames.Clear();
            Refresh();
            ChartUpdated?.Invoke();
        };

        session.ParticipantCountChanged += count =>
        {
            DateTime start = session.MeetingStartTime ?? _meetingStartedTime ?? DateTime.Now;
            double sec = Math.Max(0, (DateTime.Now - start).TotalSeconds);
            int mins = (int)(sec / 60);
            int s = (int)(sec % 60);
            GraphPoints.Add(new ParticipantDataPoint { Seconds = sec, TimeLabel = $"{mins:D2}:{s:D2}", Count = count });
            Refresh();
            ChartUpdated?.Invoke();
        };

        session.ParticipantsChanged += list =>
        {
            ParticipantList.Clear();
            var newUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var p in list)
            {
                ParticipantList.Add(p);
                if (!string.IsNullOrWhiteSpace(p.Username))
                {
                    newUsers.Add(p.Username);
                }
            }

            // Detect newly joined participants
            foreach (var username in newUsers)
            {
                if (!_activeUsernames.Contains(username))
                {
                    _activeUsernames.Add(username);
                    AuditLogs.Insert(0, new SecurityAuditEvent { User = username, Activity = "Joined meeting", Status = "Success" });
                }
            }

            // Detect participants who left
            var leftUsers = _activeUsernames.Where(u => !newUsers.Contains(u)).ToList();
            foreach (var username in leftUsers)
            {
                _activeUsernames.Remove(username);
                AuditLogs.Insert(0, new SecurityAuditEvent { User = username, Activity = "Left meeting", Status = "Info" });
            }

            if (SelectedParticipant != null)
            {
                SelectedParticipant = ParticipantList.FirstOrDefault(p => p.Username == SelectedParticipant.Username);
            }
            Refresh();
        };

        if (session.InMeeting)
        {
            _meetingStartedTime = session.MeetingStartTime ?? DateTime.Now;
            string user = !string.IsNullOrWhiteSpace(session.CurrentUserId) ? session.CurrentUserId : "Host";
            _activeUsernames.Add(user);
            foreach (var p in session.ActiveParticipants)
            {
                if (!string.IsNullOrWhiteSpace(p.Username))
                {
                    _activeUsernames.Add(p.Username);
                    ParticipantList.Add(p);
                }
            }
            if (GraphPoints.Count == 0)
            {
                GraphPoints.Add(new ParticipantDataPoint { Seconds = 0, TimeLabel = "00:00", Count = Math.Max(1, session.ParticipantCount) });
            }
            timer.Start();
        }
    }

    public double ElapsedSeconds
    {
        get
        {
            DateTime? start = session.MeetingStartTime ?? _meetingStartedTime;
            if (start.HasValue)
            {
                return Math.Max(0, (DateTime.Now - start.Value).TotalSeconds);
            }
            return GraphPoints.Count > 0 ? GraphPoints.Max(p => p.Seconds) : 0;
        }
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

    // Duration & Graph
    public string DurationText { get; private set; } = "00:00";
    public ObservableCollection<ParticipantDataPoint> GraphPoints { get; } = new();
    public ObservableCollection<SecurityAuditEvent> AuditLogs { get; } = new();
    public event Action? ChartUpdated;

    // In-meeting Security Alert Banner
    private string? _alertMessage;
    public string? AlertMessage
    {
        get => _alertMessage;
        set
        {
            _alertMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAlert));
        }
    }
    public bool HasAlert => !string.IsNullOrEmpty(_alertMessage);

    // View mode: 0 = Normal, 1 = Graph Maximized, 2 = Audit Maximized
    private int _maximizedView = 0;
    public int MaximizedView
    {
        get => _maximizedView;
        set
        {
            _maximizedView = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNormalVisible));
            OnPropertyChanged(nameof(IsGraphMaximized));
            OnPropertyChanged(nameof(IsAuditMaximized));
            OnPropertyChanged(nameof(IsOverlayVisible));
            ChartUpdated?.Invoke();
        }
    }

    public bool IsNormalVisible => _maximizedView == 0;
    public bool IsGraphMaximized => _maximizedView == 1;
    public bool IsAuditMaximized => _maximizedView == 2;
    public bool IsOverlayVisible => _maximizedView != 0;

    public void ShowNormal() => MaximizedView = 0;
    public void MaximizeGraph() => MaximizedView = 1;
    public void MaximizeAudit() => MaximizedView = 2;

    public void AddTestLog(string action, string status = "Info")
    {
        string user = !string.IsNullOrWhiteSpace(session.CurrentUserId) ? session.CurrentUserId : "User";
        AuditLogs.Insert(0, new SecurityAuditEvent { User = user, Activity = action, Status = status });
    }
}
