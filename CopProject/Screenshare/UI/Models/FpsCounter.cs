namespace ScreenShare.Models;

/// <summary>
/// counts the pictures a tile drew and turns them into frames per second. not thread-safe:
/// use it from the UI thread only.
///
/// only pictures that changed reach a tile, so a still screen shows close to 0 fps.
/// </summary>
public sealed class FpsCounter
{
    private int _frames;
    private TimeSpan _since;
    private bool _started;

    /// <summary>gets the rate measured by the last Sample</summary>
    public double FramesPerSecond { get; private set; }

    /// <summary>counts one drawn picture</summary>
    public void RecordFrame()
    {
        _frames++;
    }

    /// <summary>
    /// works out the rate since the previous sample and starts counting again.
    /// the first call only starts the clock.
    /// </summary>
    /// <param name="now">a steadily increasing time, e.g. Stopwatch elapsed time</param>
    /// <returns>the new rate</returns>
    public double Sample(TimeSpan now)
    {
        if (!_started || now <= _since)
        {
            _started = true;
            _since = now;
            _frames = 0;
            return FramesPerSecond;
        }

        FramesPerSecond = _frames / (now - _since).TotalSeconds;
        _since = now;
        _frames = 0;
        return FramesPerSecond;
    }

    /// <summary>forgets the count and the rate, e.g. when a tile is zoomed again</summary>
    public void Reset()
    {
        _frames = 0;
        _started = false;
        FramesPerSecond = 0;
    }
}
