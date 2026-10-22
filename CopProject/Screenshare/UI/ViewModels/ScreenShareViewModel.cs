using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Dispatching;
using ScreenShare.Interfaces;
using ScreenShare.Models;

namespace ScreenShare.ViewModels;

public sealed class ScreenShareViewModel : INotifyPropertyChanged
{
    private const int GridQuality = 480;
    private const int ZoomQuality = 1080;

    private readonly IScreenShareFeed _feed;
    private readonly DispatcherQueue _ui;
    private readonly DispatcherQueueTimer _feedTimer;
    private readonly Dictionary<string, ParticipantTile> _byId = new();
    private readonly HashSet<string> _onScreen = new();   // tiles currently in the viewport
    private string? _zoomedId;
    private bool _isSharingScreen;

    public ObservableCollection<ParticipantTile> Tiles { get; } = new();

    public ViewMode Mode { get; private set; } = ViewMode.Grid;
    public bool IsGridMode => Mode == ViewMode.Grid;
    public bool IsZoomedMode => Mode == ViewMode.Zoomed;
    public ParticipantTile? ZoomedTile =>
        _zoomedId != null && _byId.TryGetValue(_zoomedId, out ParticipantTile? t) ? t : null;



    public event PropertyChangedEventHandler? PropertyChanged;

    public ScreenShareViewModel(IScreenShareFeed feed, DispatcherQueue ui)
    {
        _feed = feed;
        _ui = ui;

        // Collects bursts of scroll events into one RequestFeeds call
        _feedTimer = _ui.CreateTimer();
        _feedTimer.Interval = TimeSpan.FromMilliseconds(150);
        _feedTimer.IsRepeating = false;
        _feedTimer.Tick += (_, _) => SendFeedRequests();

        // Feed events can arrive on any thread, so hop to the UI thread first.
        _feed.ParticipantJoined += (id, name) => _ui.TryEnqueue(() => AddParticipant(id, name));
        _feed.ParticipantLeft += id => _ui.TryEnqueue(() => RemoveParticipant(id));
        _feed.FrameReady += (id, bytes) => _ui.TryEnqueue(() => OnFrame(id, bytes));
    }

    // ----- Called by the View -----
    public void Zoom(string id)
    {
        if (!_byId.ContainsKey(id))
        {
            return;
        }

        _zoomedId = id;
        Mode = ViewMode.Zoomed;
        RaiseModeChanged();
        SendFeedRequests();
    }

    public void BackToGrid()
    {
        _zoomedId = null;
        Mode = ViewMode.Grid;
        RaiseModeChanged();
        SendFeedRequests();
    }

    public void TileEnteredView(ParticipantTile tile)
    {
        if (_onScreen.Add(tile.Id))
        {
            ScheduleFeedRequests();
        }
    }

    public void TileLeftView(ParticipantTile tile)
    {
        if (_onScreen.Remove(tile.Id))
        {
            ScheduleFeedRequests();
        }
    }

    // ----- Called from feed events  -----
    private void AddParticipant(string id, string name)
    {
        if (_byId.ContainsKey(id))
        {
            return;
        }

        var tile = new ParticipantTile(id, name);
        _byId[id] = tile;
        Tiles.Add(tile);             // if it lands in view, ElementPrepared fires -> TileEnteredView
    }

    private void RemoveParticipant(string id)
    {
        if (!_byId.Remove(id, out ParticipantTile? tile))
        {
            return;
        }

        Tiles.Remove(tile);
        _onScreen.Remove(id);

        if (_zoomedId == id)
        {
            _zoomedId = null;
            Mode = ViewMode.Grid;
            RaiseModeChanged();
        }
        SendFeedRequests();
    }

    private void OnFrame(string id, byte[] bytes)
    {
        bool shown = IsZoomedMode ? id == _zoomedId : _onScreen.Contains(id);
        if (shown && _byId.TryGetValue(id, out ParticipantTile? tile))
        {
            tile.UpdateFrame(bytes);
        }
    }

    // ------ User contoles --------

    public bool IsSharingScreen
    {
        get => _isSharingScreen;
        set {
            if (_isSharingScreen == value)
            {
                return;
            }

            _isSharingScreen = value;
            Raise(nameof(IsSharingScreen));
        }
    }


    // ----- Feed requests -----
    private void ScheduleFeedRequests()
    {
        _feedTimer.Stop();
        _feedTimer.Start();          // restarts the 150 ms wait
    }

    private void SendFeedRequests()
    {
        _feedTimer.Stop();
        Dictionary<string, int> wanted = IsZoomedMode && _zoomedId != null
            ? new() { [_zoomedId] = ZoomQuality }
            : _onScreen.ToDictionary(id => id, _ => GridQuality);
        _feed.RequestFeeds(wanted);
    }

    // ----- Notifications -----
    private void RaiseModeChanged()
    {
        Raise(nameof(Mode), nameof(IsGridMode), nameof(IsZoomedMode), nameof(ZoomedTile));
    }

    private void Raise(params string[] names)
    {
        foreach (string n in names)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        }
    }
}
