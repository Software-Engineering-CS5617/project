using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScreenShare.Interfaces;
using ScreenShare.Models;
using ScreenShare.ViewModels;

namespace ScreenShare.Views;

public sealed partial class ScreenShareGridView : UserControl
{
    public ScreenShareViewModel? ViewModel { get; private set; }

    public ScreenShareGridView()
    {
        InitializeComponent();
    }

    public void Initialize(IScreenShareFeed feed)
    {
        ViewModel = new ScreenShareViewModel(feed, DispatcherQueue);
        Bindings.Update();
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
