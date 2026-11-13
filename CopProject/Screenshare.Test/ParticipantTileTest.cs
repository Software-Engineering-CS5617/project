using System.ComponentModel;
using ScreenShare.Models;

namespace Screenshare.Tests;

public class ParticipantTileTests
{

    private readonly List<string?> _changedNames = new();

    private void RememberChange(object? sender, PropertyChangedEventArgs e)
    {
        _changedNames.Add(e.PropertyName);
    }

    [Fact]
    public void Constructor_StoresIdAndName()
    {
        ParticipantTile tile = new("p1", "Sumit");

        Assert.Equal("p1", tile.Id);
        Assert.Equal("Sumit", tile.Name);
    }

    [Fact]
    public void NewTileHaveNoPicture()
    {
        ParticipantTile tile = new("p2", "Sumit");

        Assert.Null(tile.Frame);
    }

    [Fact]
    public void FpsText_ShowsZero()
    {
        ParticipantTile tile = new("p1", "Sumit");

        Assert.Equal("0.0 fps", tile.FpsText);
    }

    [Fact]
    public void SampleFps_RaisesFpsTextChanged()
    {
        ParticipantTile tile = new("p1", "Alice");

        tile.PropertyChanged += RememberChange;
        tile.SampleFps(TimeSpan.Zero);

        Assert.Contains("FpsText", _changedNames);
    }


    [Fact]
    public void ResetFps_RaisesFpsTextChanged()
    {
        ParticipantTile tile = new("p1", "Sumi_it");

        tile.PropertyChanged += RememberChange;
        tile.ResetFps(TimeSpan.Zero);

        Assert.Contains("FpsText", _changedNames);
    }



}
