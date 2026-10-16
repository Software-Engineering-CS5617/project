using System.Drawing;

namespace ScreenShare;

public class StreamFrameCache : IDisposable
{
    private readonly Dictionary<string, Bitmap> frames = new();

    private readonly object cacheLock = new();

    private bool disposed;

    public void Update(string streamId, Bitmap frame)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentNullException.ThrowIfNull(frame);

        // make a copy so that caller retains ownership of its bitmap.
        Bitmap copy = new Bitmap(frame);

        lock (cacheLock)
        {
            if (disposed)
            {
                copy.Dispose();
                throw new ObjectDisposedException(nameof(StreamFrameCache));
            }

            if (frames.TryGetValue(streamId, out Bitmap? oldFrame))
            {
                frames[streamId] = copy;
                oldFrame.Dispose();
            }
            else
            {
                frames.Add(streamId, copy);
            }
        }
    }

    public Bitmap? Get(string streamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        lock (cacheLock)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(StreamFrameCache));

            return frames.TryGetValue(streamId, out Bitmap? frame) ? new Bitmap(frame) : null;
        }
    }

    public void Remove(string streamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        lock (cacheLock)
        {
            if (disposed)
                throw new ObjectDisposedException( nameof(StreamFrameCache));

            if (frames.Remove(streamId, out Bitmap? frame))
            {
                frame.Dispose();
            }
        }
    }

    public void Dispose()
    {
        lock (cacheLock)
        {
            if (disposed)
                return;

            disposed = true;

            foreach (Bitmap frame in frames.Values)
            {
                frame.Dispose();
            }

            frames.Clear();
        }
    }
}
