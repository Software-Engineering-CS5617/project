using System.ComponentModel;
using ScreenShare.Models;
using Screenshare.Tests.Fakes;
using Windows.ApplicationModel.VoiceCommands;
using Windows.UI.ViewManagement;

namespace Screenshare.Tests;

public class ParticipantTileTests
{

    private readonly List<string?> _changedNames = new();

    private readonly List<FakeTileSurface> _surfaces = new();

    private void RememberChange(object? sender, PropertyChangedEventArgs e)
    {
        _changedNames.Add(e.PropertyName);
    }

    private ParticipantTile NewTileWithFakeSurface()
    {
        return new ParticipantTile("p1", "Sumit", (width, height) => {
            FakeTileSurface surface = new(width, height);
            _surfaces.Add(surface);
            return surface;
        });
    }

    private static ScreenFrame Frame(int width, int height)
    {
        return new ScreenFrame(width, height, new byte[width * height * 4]);
    }

    [Fact]
    public void Constructor_StoresIdAndName()
    {
        ParticipantTile tile = new("p1", "Sumit");

        Assert.Equal("p1", tile.Id);
        Assert.Equal("Sumit", tile.Name);
    }

    [Fact]
    public void Constructor_NullSurfaceFactory_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ParticipantTile("p1", "Sumit", null!));
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

    [Fact]
    public void UpdateFrame_FirstFrame_CreatesSurfaceOfFrameSize()
    {
        ParticipantTile tile = NewTileWithFakeSurface();

        tile.UpdateFrame(Frame(4, 3));

        FakeTileSurface surface = Assert.Single(_surfaces);
        Assert.Equal(4, surface.PixelWidth);
        Assert.Equal(3, surface.PixelHeight);
    }

    [Fact]
    public void UpdateFrame_FirstFrame_RaisesFrameChangedOnce()
    {
        ParticipantTile tile = NewTileWithFakeSurface();
        tile.PropertyChanged += RememberChange;

        tile.UpdateFrame(Frame(4, 3));

        Assert.Equal(new string?[] { "Frame" }, _changedNames);
    }

    [Fact]
    public void UpdateFrame_WritesFramePixelsToSurface()
    {
        ParticipantTile tile = NewTileWithFakeSurface();
        ScreenFrame frame = Frame(2, 2);

        tile.UpdateFrame(frame);

        Assert.Same(frame.Pixels, Assert.Single(_surfaces[0].Writes));
    }

    [Fact]
    public void UpdateFrame_SameSize_ReusesSurfaceWithoutFrameChanged()
    {
        ParticipantTile tile = NewTileWithFakeSurface();
        tile.UpdateFrame(Frame(4, 3));
        tile.PropertyChanged += RememberChange;

        tile.UpdateFrame(Frame(4, 3));

        FakeTileSurface surface = Assert.Single(_surfaces);
        Assert.Equal(2, surface.Writes.Count);
        Assert.Empty(_changedNames);
    }

    [Theory]
    [InlineData(5, 3)] // only the width changed
    [InlineData(4, 6)] // only the height changed
    public void UpdateFrame_NewSize_ReplacesSurface(int width, int height)
    {
        ParticipantTile tile = NewTileWithFakeSurface();
        tile.UpdateFrame(Frame(4, 3));
        tile.PropertyChanged += RememberChange;

        tile.UpdateFrame(Frame(width, height));

        Assert.Equal(2, _surfaces.Count);
        Assert.Equal(width, _surfaces[1].PixelWidth);
        Assert.Equal(height, _surfaces[1].PixelHeight);
        Assert.Single(_surfaces[1].Writes);
        Assert.Equal(new string?[] { "Frame" }, _changedNames);
    }

    [Fact]
    public void UpdateFrame_DrawnFramesCountTowardFps()
    {
        ParticipantTile tile = NewTileWithFakeSurface();
        tile.ResetFps(TimeSpan.Zero);

        tile.UpdateFrame(Frame(2, 2));
        tile.UpdateFrame(Frame(2, 2));
        tile.UpdateFrame(Frame(2, 2));
        tile.SampleFps(TimeSpan.FromSeconds(1));

        Assert.Equal("3.0 fps", tile.FpsText);
    }

    [Fact]
    public void UpdateFrame_SurfaceWriteFails_DoesNotThrowOrCountFrame()
    {
        ParticipantTile tile = NewTileWithFakeSurface();
        tile.UpdateFrame(Frame(2, 2));
        _surfaces[0].FailWrites = true;
        tile.ResetFps(TimeSpan.Zero);

        tile.UpdateFrame(Frame(2, 2));
        tile.SampleFps(TimeSpan.FromSeconds(1));

        Assert.Equal("0.0 fps", tile.FpsText);
    }

    [Fact]
    public void UpdateFrame_SurfaceCreationFails_KeepsOldSurface()
    {
        bool failNext = false;
        int created = 0;
        ParticipantTile tile = new("p1", "Sumit", (width, height) => {
            if (failNext)
            {
                throw new InvalidOperationException("no bitmap");
            }

            created++;
            return new FakeTileSurface(width, height);
        });
        tile.UpdateFrame(Frame(2, 2));
        failNext = true;
        tile.PropertyChanged += RememberChange;

        tile.UpdateFrame(Frame(3, 3));

        Assert.Equal(1, created);
        Assert.Empty(_changedNames);
    }

    [Fact]
    public void UpdateFrame_AfterFailure_NextFrameIsDrawn()
    {
        ParticipantTile tile = NewTileWithFakeSurface();
        tile.UpdateFrame(Frame(2, 2));
        _surfaces[0].FailWrites = true;
        tile.UpdateFrame(Frame(2, 2));
        _surfaces[0].FailWrites = false;

        tile.UpdateFrame(Frame(2, 2));

        Assert.Equal(2, _surfaces[0].Writes.Count);
    }
}
