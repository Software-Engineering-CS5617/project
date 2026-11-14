using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ScreenShare.Models;

public class ParticipantTile : INotifyPropertyChanged
{
    private WriteableBitmap? _bitmap;
    private readonly FpsCounter _fps = new();

    public string Id { get; }
    public string Name { get; }

    // null until the first frame arrives; replaced only when the stream's size changes
    public ImageSource? Frame => _bitmap;

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

    public ParticipantTile(string id, string name) { Id = id; Name = name; }

    // Call on the UI thread. Copies the raw pixels straight into the bitmap, no decoding needed.
    public void UpdateFrame(ScreenFrame frame)
    {
        try
        {
            if (_bitmap == null || _bitmap.PixelWidth != frame.Width || _bitmap.PixelHeight != frame.Height)
            {
                _bitmap = new WriteableBitmap(frame.Width, frame.Height);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Frame)));
            }

            using (Stream pixels = _bitmap.PixelBuffer.AsStream())
            {
                pixels.Write(frame.Pixels, 0, frame.Pixels.Length);
            }

            _bitmap.Invalidate();
            _fps.RecordFrame();
        }
        catch (Exception e)
        {
            // one bad frame should not take down the meeting window; the next frame replaces it
            Trace.WriteLine($"[ScreenShare] could not show a frame for {Id}: {e.Message}");
        }
    }
}
