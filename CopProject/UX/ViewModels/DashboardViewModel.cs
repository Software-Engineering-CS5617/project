using System;
using System.Collections.ObjectModel;
using System.IO;
using SecureMeet.Shared.Models;
using SecureMeet.Shared.Modules;
using SecureMeet.Shared.Mvvm;
using UX.Models;

namespace UX.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    readonly MeetingSession session;
    readonly IMeetingService meetingService;
    readonly string historyFile;
    MeetingInfo? current;
    DateTime startedAt;

    public DashboardViewModel(MeetingSession session, IMeetingService meetingService, string folder)
    {
        this.session = session;
        this.meetingService = meetingService;
        historyFile = Path.Combine(folder, "history.txt");

        StartCommand = new RelayCommand(Start);
        IncidentsCommand = new RelayCommand(() => session.IncidentsOpen = true);

        session.UserSignedIn += _ => Refresh();
        session.ProfileChanged += _ => Refresh();
        session.MeetingStarted += meeting => { current = meeting; startedAt = DateTime.Now; };
        session.MeetingEnded += AddToHistory;

        try
        {
            if (File.Exists(historyFile))
                foreach (string line in File.ReadAllLines(historyFile)) History.Add(line);
        }
        catch (Exception) { }
    }

    public string Greeting
    {
        get
        {
            int hour = DateTime.Now.Hour;
            string text = hour < 12 ? "Good Morning" : hour < 17 ? "Good Afternoon" : "Good Evening";
            return text + ", " + session.CurrentUser?.UserId + "!";
        }
    }

    public string MeetingId { get; set; } = "";
    public string Port { get; set; } = "";
    public string Error { get; set; } = "";
    public bool HasError => Error != "";

    public ObservableCollection<string> History { get; } = new();
    public bool HasNoHistory => History.Count == 0;

    public RelayCommand StartCommand { get; }
    public RelayCommand IncidentsCommand { get; }

    // CONTROLLER TEAM: your IMeetingService creates the real ID and port.
    void Start()
    {
        var (id, port) = meetingService.CreateMeeting();
        session.StartMeeting(new MeetingInfo(id, port, IsHost: true));
    }

    public void ClearJoinForm()
    {
        MeetingId = "";
        Port = "";
        Error = "";
        Refresh();
    }

    // CONTROLLER TEAM: connect in your MeetingStarted handler.
    public bool TryJoin()
    {
        string id = MeetingId.Trim();
        string portText = Port.Trim();

        if (id == "")
            Error = "Please enter the meeting ID.";
        else if (!int.TryParse(portText, out int number) || number < 1 || number > 65535)
            Error = "Port must be a number between 1 and 65535.";
        else
        {
            session.StartMeeting(new MeetingInfo(id, portText, IsHost: false));
            return true;
        }

        Refresh();
        return false;
    }

    void AddToHistory()
    {
        if (current == null) return;

        int minutes = (int)(DateTime.Now - startedAt).TotalMinutes;
        string length = minutes < 1 ? "under 1 min" : minutes + " min";
        string role = current.IsHost ? "Hosted" : "Joined";
        string line = role + " meeting " + current.Id + " (port " + current.Port + ") - "
                      + startedAt.ToString("ddd, dd MMM, HH:mm") + " - " + length;

        History.Insert(0, line);
        try { File.WriteAllLines(historyFile, History); } catch (Exception) { }
        Refresh();
    }
}
