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

    public MeetingViewModel(MeetingSession session)
    {
        this.session = session;
        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) =>
        {
            if (session.InMeeting && session.MeetingStartTime.HasValue)
            {
                var elapsed = DateTime.Now - session.MeetingStartTime.Value;
                DurationText = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
                OnPropertyChanged(nameof(DurationText));
                ChartUpdated?.Invoke();
            }
        };

        session.MeetingStarted += _ => 
        {
            string user = !string.IsNullOrWhiteSpace(session.CurrentUserId) ? session.CurrentUserId : "Host";
            AuditLogs.Insert(0, new SecurityAuditEvent { User = user, Activity = "Meeting created & started", Status = "Success" });
            AuditLogs.Insert(0, new SecurityAuditEvent { User = user, Activity = "User logged in", Status = "Success" });

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
            Refresh();
            ChartUpdated?.Invoke();
        };
        session.ParticipantCountChanged += count => 
        {
            if (session.MeetingStartTime.HasValue)
            {
                double sec = (DateTime.Now - session.MeetingStartTime.Value).TotalSeconds;
                int mins = (int)(sec / 60);
                int s = (int)(sec % 60);
                GraphPoints.Add(new ParticipantDataPoint { Seconds = sec, TimeLabel = $"{mins:D2}:{s:D2}", Count = count });
            }
            Refresh();
            ChartUpdated?.Invoke();
        };
        session.ParticipantsChanged += list =>
        {
            ParticipantList.Clear();
            foreach (var p in list)
            {
                ParticipantList.Add(p);
                if (!string.IsNullOrWhiteSpace(p.Username))
                {
                    bool exists = AuditLogs.Any(a => a.User == p.Username && a.Activity.Contains("joined"));
                    if (!exists)
                    {
                        AuditLogs.Insert(0, new SecurityAuditEvent { User = p.Username, Activity = "Joined meeting", Status = "Success" });
                    }
                }
            }

            if (SelectedParticipant != null)
            {
                SelectedParticipant = ParticipantList.FirstOrDefault(p => p.Username == SelectedParticipant.Username);
            }
            Refresh();
        };
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