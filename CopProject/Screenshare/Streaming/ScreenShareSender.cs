using System.Diagnostics;

namespace ScreenShare;

internal sealed class ScreenShareSender : IDisposable
{
    private readonly Participant _local;
    private readonly string _displayName;
    private readonly IImageSource _source;
    private readonly ScreenSharingService _service;
    private readonly ScreenPacketizer _packetizer;
    private readonly Action<IScreenShareMessage> _send;
    private readonly ScreenShareOptions _options;
    private readonly TimeProvider _timeProvider;

    private readonly object _flowLock = new();
    private long _lastSent = -1;
    private long _lastAcked = -1;
    private DateTimeOffset _lastProgress;

    private volatile bool _fullFrameRequested;
    private bool _announced;
    private volatile bool _stopped;
    private CancellationTokenSource? _loopCancellation;
    private Thread? _loopThread;
    public string StreamId => _local.Id;

    public ScreenShareSender(
        Participant local,
        string displayName,
        IImageSource source,
        Action<IScreenShareMessage> send,
        ScreenShareOptions options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(send);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _local = local;
        _displayName = displayName;
        _source = source;
        _service = new ScreenSharingService(source, new ImageDiffer());
        _packetizer = new ScreenPacketizer(local.Id);
        _send = send;
        _options = options;
        _timeProvider = timeProvider;
        _lastProgress = timeProvider.GetUtcNow();
    }

    public bool CaptureOnce()
    {
        if (_stopped || !MaySendFrame())
        {
            return false;
        }

        if (_fullFrameRequested)
        {
            _fullFrameRequested = false;
            _service.Reset();
        }

        using ImageDiffResult result = _service.ProcessFrame();
        ScreenDataPacket packet = _packetizer.CreatePacket(result, _timeProvider.GetUtcNow().UtcDateTime);

        if (!_announced)
        {
            _send(new StreamStarted(StreamId, _local, _displayName, packet.Width, packet.Height));
            _announced = true;
        }

        lock (_flowLock)
        {
            _lastSent = packet.FrameNumber;
        }

        _send(packet);
        return true;
    }

    private bool MaySendFrame()
    {
        lock (_flowLock)
        {
            if (_lastSent - _lastAcked < _options.MaxFramesInFlight)
            {
                return true;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (now - _lastProgress < _options.AckTimeout)
            {
                return false;
            }

            // ack timeout, start over
            Trace.WriteLine($"[ScreenShare] {StreamId}: no ack for {_options.AckTimeout}, resending a full frame");
            _lastAcked = _lastSent;
            _lastProgress = now;
            _fullFrameRequested = true;
            return true;
        }
    }

    private void RunLoop(CancellationToken token)
    {
        TimeSpan interval = TimeSpan.FromSeconds(1.0 / _options.FramesPerSecond);
        var stopwatch = new Stopwatch();

        while (!token.IsCancellationRequested)
        {
            stopwatch.Restart();

            try
            {
                CaptureOnce();
            }
            catch (Exception e)
            {
                Trace.WriteLine($"[ScreenShare] {StreamId}: capture failed: {e.Message}");
            }

            TimeSpan remaining = interval - stopwatch.Elapsed;
            if (remaining > TimeSpan.Zero)
            {
                token.WaitHandle.WaitOne(remaining);
            }
        }
    }

    public void StartLoop()
    {
        if (_loopThread != null || _stopped)
        {
            return;
        }

        _loopCancellation = new CancellationTokenSource();
        CancellationToken token = _loopCancellation.Token;

        _loopThread = new Thread(() => RunLoop(token)) {
            IsBackground = true,
            Name = $"ScreenShare capture {StreamId}"
        };
        _loopThread.Start();
    }

    public void OnFrameAck(long frameNumber)
    {
        lock (_flowLock)
        {
            if (frameNumber > _lastAcked && frameNumber <= _lastSent)
            {
                _lastAcked = frameNumber;
                _lastProgress = _timeProvider.GetUtcNow();
            }
        }
    }

    public void OnFullFrameRequested()
    {
        _fullFrameRequested = true;
    }

    public void Stop()
    {
        if (_stopped)
        {
            return;
        }

        _stopped = true;
        _loopCancellation?.Cancel();

        if (_loopThread != null && Thread.CurrentThread != _loopThread)
        {
            _loopThread.Join();
        }

        if (_announced)
        {
            _send(new StreamStopped(StreamId));
        }
    }

    public void Dispose()
    {
        Stop();
        _loopCancellation?.Dispose();
        _service.Dispose();
        (_source as IDisposable)?.Dispose();
    }
}
