using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScreenShare.Interfaces;
using ScreenShare.Models;
using ScreenShare.ViewModels;

namespace ScreenShare.Views;

public sealed partial class ScreenShareGridView : UserControl, IScreenShareView
{
    public ScreenShareViewModel? ViewModel { get; private set; }

    public ScreenShareGridView()
    {
        InitializeComponent();
    }

    /// <summary>shows a feed's streams; any previous feed is let go first. call on the UI thread.</summary>
    public void Initialize(IScreenShareFeed feed)
    {
        ViewModel?.Dispose();
        ViewModel = new ScreenShareViewModel(feed, DispatcherQueue);
        Bindings.Update();
    }

    /// <summary>
    /// empties the grid and lets go of the feed, e.g. when the meeting ends. the emptied view
    /// model stays bound so the grid shows nothing until the next Initialize.
    /// </summary>
    public void Clear()
    {
        ViewModel?.Dispose();
    }

    void IScreenShareView.Show(IScreenShareFeed feed)
    {
        Initialize(feed);
    }

    // ----- Tile visibility (drives which feeds are requested) -----
    private void TileGrid_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Item is not ParticipantTile tile)
        {
            return;
        }

        if (args.InRecycleQueue)
        {
            ViewModel?.TileLeftView(tile);      // tile scrolled away or was removed
        }
        else
        {
            ViewModel?.TileEnteredView(tile);   // tile is being shown
        }
        // Don't set args.Handled = true, because x:Bind in the item template relies on this event too.
    }

    // ----- Zoom in / out -----
    private void Tile_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ParticipantTile tile)
        {
            ViewModel?.Zoom(tile.Id);
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.BackToGrid();
    }


}
