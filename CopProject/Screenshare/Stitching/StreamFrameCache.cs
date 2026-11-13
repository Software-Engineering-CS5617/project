using System.Drawing;

namespace ScreenShare;

public class StreamFrameCache : IDisposable
{
    private readonly Dictionary<string, Bitmap> _frames = new();

    private readonly object _cacheLock = new();

    private bool _disposed;

    public void Update(string streamId, Bitmap frame)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentNullException.ThrowIfNull(frame);

        // make a copy so that caller retains ownership of its bitmap.
        Bitmap copy = new Bitmap(frame);

        lock (_cacheLock)
        {
            if (_disposed)
            {
                copy.Dispose();
                throw new ObjectDisposedException(nameof(StreamFrameCache));
            }

            if (_frames.TryGetValue(streamId, out Bitmap? oldFrame))
            {
                _frames[streamId] = copy;
                oldFrame.Dispose();
            }
            else
            {
                _frames.Add(streamId, copy);
            }
        }
    }

    public Bitmap? Get(string streamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        lock (_cacheLock)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(StreamFrameCache));
            }

            return _frames.TryGetValue(streamId, out Bitmap? frame) ? new Bitmap(frame) : null;
        }
    }

    public void Remove(string streamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        lock (_cacheLock)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(StreamFrameCache));
            }

            if (_frames.Remove(streamId, out Bitmap? frame))
            {
                frame.Dispose();
            }
        }
    }

    public void Dispose()
    {
        lock (_cacheLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (Bitmap frame in _frames.Values)
            {
                frame.Dispose();
            }

            _frames.Clear();
        }
    }
}
