using System.ComponentModel;
using ScreenShare.Models;
using Windows.ApplicationModel.VoiceCommands;
using Windows.UI.ViewManagement;

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

    [Fact]
    public void ResetFps_ShowsZeroFps()
    {
        ParticipantTile tile = new("1", "Sumit");

        tile.SampleFps(TimeSpan.FromSeconds(0));
        tile.SampleFps(TimeSpan.FromSeconds(1));
        tile.ResetFps(TimeSpan.FromSeconds(2));

        Assert.Equal("0.0 fps", tile.FpsText);
    }

    [Fact]
    public void UpdateFrame_NeverThrows()
    {
        ParticipantTile tile = new("1", "sumit");
        byte[] pixels = new byte[10 * 20 * 4];
        ScreenFrame frame = new(10, 20, pixels);

        tile.UpdateFrame(frame);
    }


    [Fact]
    public void FrameCouldNotDraw_IsNotCounted()
    {
        ParticipantTile tile = new("10", "Sumit");
        byte[] pixels = new byte[10 * 20 * 4];
        ScreenFrame frame = new(10, 20, pixels); ;

        tile.SampleFps(TimeSpan.Zero);
        for (int i = 0; i < 10; i++)
        {
            tile.UpdateFrame(frame);
        }

        tile.SampleFps(TimeSpan.FromSeconds(1));

        Assert.Equal("0.0 fps", tile.FpsText);
        Assert.Null(tile.Frame);



    }

    [Fact]
    public void NullFramePassedToUpadateFrame()
    {
        ParticipantTile tile = new("10", "Sumit");

        tile.SampleFps(TimeSpan.Zero);
        for (int i = 10; i < 50; i += 10)
        {
            byte[] pixels = new byte[(i + 10) * 20 * 4];
            ScreenFrame frame = new(i + 10, 20, pixels);
            tile.UpdateFrame(frame);
        }
        tile.SampleFps(TimeSpan.FromSeconds(1));

        //Assert.Equal("5.0 fps", tile.FpsText);
        Assert.Null(tile.Frame);

    }



}
