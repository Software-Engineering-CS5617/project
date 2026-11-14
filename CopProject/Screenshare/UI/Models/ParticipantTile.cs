using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media;

namespace ScreenShare.Models;

public class ParticipantTile : INotifyPropertyChanged
{
    private readonly Func<int, int, ITileSurface> _createSurface;
    private ITileSurface? _surface;
    private readonly FpsCounter _fps = new();

    public string Id { get; }
    public string Name { get; }

    // null until the first frame arrives; replaced only when the stream's size changes
    public ImageSource? Frame => _surface?.Source;

    /// <summary>pictures drawn per second, as of the last SampleFps, e.g. "23.8 fps"</summary>
    public string FpsText => $"{_fps.FramesPerSecond:0.0} fps";

    /// <summary>updates FpsText from the pictures drawn since the last call. UI thread only.</summary>
    public void SampleFps(TimeSpan now)
    {
        _fps.Sample(now);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FpsText)));
    }

    /// <summary>starts counting afresh, e.g. when the tile is zoomed. UI thread only.</summary>
    public void ResetFps(TimeSpan now)
    {
        _fps.Reset();
        _fps.Sample(now);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FpsText)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ParticipantTile(string id, string name)
        : this(id, name, (width, height) => new BitmapTileSurface(width, height))
    {
    }

    /// <param name="createSurface">makes the picture for a given width and height</param>
    /// <exception cref="ArgumentNullException">createSurface is null</exception>
    internal ParticipantTile(string id, string name, Func<int, int, ITileSurface> createSurface)
    {
        ArgumentNullException.ThrowIfNull(createSurface);

        Id = id;
        Name = name;
        _createSurface = createSurface;
    }

    // Call on the UI thread.
    public void UpdateFrame(ScreenFrame frame)
    {
        try
        {
            if (_surface == null || _surface.PixelWidth != frame.Width || _surface.PixelHeight != frame.Height)
            {
                _surface = _createSurface(frame.Width, frame.Height);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Frame)));
            }

            _surface.WritePixels(frame.Pixels);
            _fps.RecordFrame();
        }
        catch (Exception e)
        {
            // one bad frame should not take down the meeting window; the next frame replaces it
            Trace.WriteLine($"[ScreenShare] could not show a frame for {Id}: {e.Message}");
        }
    }
}
