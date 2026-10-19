using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
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

    // The host app calls this once with the real (or fake) feed.
    public void Initialize(IScreenShareFeed feed)
    {
        ViewModel = new ScreenShareViewModel(feed, DispatcherQueue);
        DataContext = ViewModel; // use standard {Binding} in XAML
    }

    private void Tile_Click(object sender, ItemClickEventArgs e)
    {
        ViewModel?.Zoom(((ParticipantTile)e.ClickedItem).Id);
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.BackToGrid();
    }

    private void Prev_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.PrevPage();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.NextPage();
    }

    // Size tiles so 4 columns x 2 rows fill whatever space the host gives us.
    private void TileGrid_Loaded(object sender, RoutedEventArgs e)
    {
        ResizeTiles();
    }

    private void TileGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ResizeTiles();
    }

    private void ResizeTiles()
    {
        if (TileGrid.ItemsPanelRoot is ItemsWrapGrid panel && TileGrid.ActualWidth > 0)
        {
            panel.ItemWidth = Math.Floor(TileGrid.ActualWidth / 4) - 1;
            panel.ItemHeight = Math.Floor(TileGrid.ActualHeight / 2) - 1;
        }
    }
}
