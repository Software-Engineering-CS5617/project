using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Dispatching;
using ScreenShare.Interfaces;
using ScreenShare.Models;

namespace ScreenShare.ViewModels;

public sealed class ScreenShareViewModel : INotifyPropertyChanged
{
    private const int PageSize = 8;
    private const int GridQuality = 480;
    private const int ZoomQuality = 1080;
    private readonly IScreenShareFeed _feed;
    private readonly DispatcherQueue _ui;
    private readonly List<ParticipantTile> _all = new();
    private Dictionary<string, ParticipantTile> _byId = new();
    private string? _zoomedId;

    public ObservableCollection<ParticipantTile> VisibleTiles { get; } = new();

    public ViewMode Mode { get; private set; } = ViewMode.Grid;
    public int CurrentPage { get; private set; }
    public int PageCount => Math.Max(1, (_all.Count + PageSize - 1) / PageSize);

    // Convenience properties for the XAML
    public bool IsGridMode => Mode == ViewMode.Grid;
    public bool IsZoomedMode => Mode == ViewMode.Zoomed;
    public bool HasMultiplePages => PageCount > 1;
    public bool CanGoPrev => CurrentPage > 0;
    public bool CanGoNext => CurrentPage < PageCount - 1;
    public string PageLabel => $"Page {CurrentPage + 1} of {PageCount}";
    public ParticipantTile? ZoomedTile => IsZoomedMode ? VisibleTiles.FirstOrDefault() : null;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ScreenShareViewModel(IScreenShareFeed feed, DispatcherQueue ui)
    {
        _feed = feed;
        _ui = ui;

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
        Refresh();
    }
    public void BackToGrid() { _zoomedId = null; Mode = ViewMode.Grid; Refresh(); }
    public void NextPage() { if (CanGoNext) { CurrentPage++; Refresh(); } }
    public void PrevPage() { if (CanGoPrev) { CurrentPage--; Refresh(); } }

    // ----- Called from feed events (already on UI thread) -----
    private void AddParticipant(string id, string name)
    {
        if (_byId.ContainsKey(id))
        {
            return;
        }

        var tile = new ParticipantTile(id, name);
        _all.Add(tile);
        _byId[id] = tile;
        Refresh();
    }

    private void RemoveParticipant(string id)
    {
        if (!_byId.Remove(id, out ParticipantTile? tile))
        {
            return;
        }

        _all.Remove(tile);
        if (_zoomedId == id) { _zoomedId = null; Mode = ViewMode.Grid; }
        Refresh();
    }

    private void OnFrame(string id, byte[] bytes)
    {
        if (_byId.TryGetValue(id, out ParticipantTile? tile) && VisibleTiles.Contains(tile))
        {
            tile.UpdateFrame(bytes);
        }
    }

    // ----- The one place that decides what is on screen -----
    private void Refresh()
    {
        CurrentPage = Math.Clamp(CurrentPage, 0, PageCount - 1);

        var wanted = (IsZoomedMode
            ? _all.Where(t => t.Id == _zoomedId)
            : _all.Skip(CurrentPage * PageSize).Take(PageSize)).ToList();

        VisibleTiles.Clear();
        foreach (ParticipantTile t in wanted)
        {
            VisibleTiles.Add(t);
        }

        int quality = IsZoomedMode ? ZoomQuality : GridQuality;
        _feed.RequestFeeds(wanted.ToDictionary(t => t.Id, _ => quality));

        string[] array = new[] { nameof(Mode), nameof(IsGridMode), nameof(IsZoomedMode),
                                     nameof(CurrentPage), nameof(PageCount), nameof(HasMultiplePages),
                                     nameof(CanGoPrev), nameof(CanGoNext), nameof(PageLabel),
                                     nameof(ZoomedTile) };
        for (int i = 0; i < array.Length; i++)
        {
            string name = array[i];
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
