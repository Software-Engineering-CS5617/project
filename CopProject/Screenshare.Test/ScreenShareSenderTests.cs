using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using ScreenShare;

namespace Screenshare.Tests;

public class ScreenShareSenderTests
{
    private static Participant CreateLocalParticipant()
    {
        return new Participant("stream-1", IPEndPoint.Parse("127.0.0.1:5000"));
    }

    private static ScreenShareOptions CreateDefaultOptions()
    {
        return new ScreenShareOptions {
            MaxFramesInFlight = 2,
            AckTimeout = TimeSpan.FromSeconds(5),
            FramesPerSecond = 10
        };
    }

    [Fact]
    public void Constructor_InvalidArguments_ThrowsAndSetsStreamId()
    {
        Participant local = CreateLocalParticipant();
        IImageSource source = new FakeImageSource();
        Action<IScreenShareMessage> send = _ => { };
        ScreenShareOptions options = CreateDefaultOptions();
        TimeProvider timeProvider = new ManualTimeProvider();

        Assert.Throws<ArgumentNullException>(() =>
            new ScreenShareSender(null!, "Display", source, send, options, timeProvider));
        Assert.Throws<ArgumentNullException>(() =>
            new ScreenShareSender(local, null!, source, send, options, timeProvider));
        Assert.Throws<ArgumentException>(() =>
            new ScreenShareSender(local, "", source, send, options, timeProvider));
        Assert.Throws<ArgumentException>(() =>
            new ScreenShareSender(local, "   ", source, send, options, timeProvider));
        Assert.Throws<ArgumentNullException>(() =>
            new ScreenShareSender(local, "Display", null!, send, options, timeProvider));
        Assert.Throws<ArgumentNullException>(() =>
            new ScreenShareSender(local, "Display", source, null!, options, timeProvider));
        Assert.Throws<ArgumentNullException>(() =>
            new ScreenShareSender(local, "Display", source, send, null!, timeProvider));
        Assert.Throws<ArgumentNullException>(() =>
            new ScreenShareSender(local, "Display", source, send, options, null!));

        using ScreenShareSender sender = new ScreenShareSender(
            local, "Display", source, send, options, timeProvider);

        Assert.Equal("stream-1", sender.StreamId);
    }

    [Fact]
    public void CaptureOnce_ConsecutiveCaptures_AnnouncesOnceAndSendsDataPackets()
    {
        Participant local = CreateLocalParticipant();
        FakeImageSource source = new FakeImageSource();
        List<IScreenShareMessage> sentMessages = new List<IScreenShareMessage>();
        ScreenShareOptions options = CreateDefaultOptions();
        ManualTimeProvider timeProvider = new ManualTimeProvider();

        using ScreenShareSender sender = new ScreenShareSender(
            local, "Main Screen", source, sentMessages.Add, options, timeProvider);

        bool firstCapture = sender.CaptureOnce();
        bool secondCapture = sender.CaptureOnce();

        Assert.True(firstCapture);
        Assert.True(secondCapture);
        Assert.Equal(3, sentMessages.Count);
        Assert.IsType<StreamStarted>(sentMessages[0]);
        Assert.IsType<ScreenDataPacket>(sentMessages[1]);
        Assert.IsType<ScreenDataPacket>(sentMessages[2]);
    }

    [Fact]
    public void CaptureOnce_FullFrameRequested_ResetsServiceAndCapturesFrame()
    {
        Participant local = CreateLocalParticipant();
        FakeImageSource source = new FakeImageSource();
        List<IScreenShareMessage> sentMessages = new List<IScreenShareMessage>();
        ScreenShareOptions options = CreateDefaultOptions();
        ManualTimeProvider timeProvider = new ManualTimeProvider();

        using ScreenShareSender sender = new ScreenShareSender(
            local, "Main Screen", source, sentMessages.Add, options, timeProvider);

        sender.OnFullFrameRequested();
        bool captured = sender.CaptureOnce();

        Assert.True(captured);
        Assert.Equal(2, sentMessages.Count);
    }

    [Fact]
    public void CaptureOnce_MaxFramesInFlightReached_BlocksUntilAckTimeoutThenRecovers()
    {
        Participant local = CreateLocalParticipant();
        FakeImageSource source = new FakeImageSource();
        List<IScreenShareMessage> sentMessages = new List<IScreenShareMessage>();
        ScreenShareOptions options = new ScreenShareOptions {
            MaxFramesInFlight = 1,
            AckTimeout = TimeSpan.FromSeconds(5),
            FramesPerSecond = 10
        };
        ManualTimeProvider timeProvider = new ManualTimeProvider();

        using ScreenShareSender sender = new ScreenShareSender(
            local, "Main Screen", source, sentMessages.Add, options, timeProvider);

        Assert.True(sender.CaptureOnce());

        timeProvider.Advance(TimeSpan.FromSeconds(2));
        Assert.False(sender.CaptureOnce());

        timeProvider.Advance(TimeSpan.FromSeconds(4));
        Assert.True(sender.CaptureOnce());
    }

    [Fact]
    public void OnFrameAck_ValidAndOutOfRangeNumbers_UpdatesInFlightWindow()
    {
        Participant local = CreateLocalParticipant();
        FakeImageSource source = new FakeImageSource();
        List<IScreenShareMessage> sentMessages = new List<IScreenShareMessage>();
        ScreenShareOptions options = new ScreenShareOptions {
            MaxFramesInFlight = 1,
            AckTimeout = TimeSpan.FromSeconds(30),
            FramesPerSecond = 10
        };
        ManualTimeProvider timeProvider = new ManualTimeProvider();

        using ScreenShareSender sender = new ScreenShareSender(
            local, "Main Screen", source, sentMessages.Add, options, timeProvider);

        Assert.True(sender.CaptureOnce());
        Assert.False(sender.CaptureOnce());

        sender.OnFrameAck(-1);
        sender.OnFrameAck(99);
        Assert.False(sender.CaptureOnce());

        sender.OnFrameAck(0);
        Assert.True(sender.CaptureOnce());
    }

    [Fact]
    public void StartLoop_HandlesRunningLoopExceptionsDuplicateStartsAndStop()
    {
        Participant local = CreateLocalParticipant();
        FakeImageSource source = new FakeImageSource {
            ExceptionToThrow = new InvalidOperationException("Capture failure")
        };
        List<IScreenShareMessage> sentMessages = new List<IScreenShareMessage>();
        ScreenShareOptions options = new ScreenShareOptions {
            MaxFramesInFlight = 2,
            AckTimeout = TimeSpan.FromSeconds(5),
            FramesPerSecond = 2
        };
        ManualTimeProvider timeProvider = new ManualTimeProvider();

        using ScreenShareSender sender = new ScreenShareSender(
            local, "Main Screen", source, sentMessages.Add, options, timeProvider);

        sender.StartLoop();
        sender.StartLoop();

        Thread.Sleep(50);

        sender.Stop();
        sender.StartLoop();

        Assert.False(sender.CaptureOnce());
    }

    [Fact]
    public void Stop_UnannouncedAndAnnouncedStates_SendsStreamStoppedOnlyWhenAnnounced()
    {
        Participant local = CreateLocalParticipant();
        FakeImageSource source = new FakeImageSource();
        ScreenShareOptions options = CreateDefaultOptions();
        ManualTimeProvider timeProvider = new ManualTimeProvider();

        List<IScreenShareMessage> unannouncedMessages = new List<IScreenShareMessage>();
        ScreenShareSender unannouncedSender = new ScreenShareSender(
            local, "Main Screen", source, unannouncedMessages.Add, options, timeProvider);

        unannouncedSender.Stop();
        unannouncedSender.Stop();
        Assert.Empty(unannouncedMessages);

        List<IScreenShareMessage> announcedMessages = new List<IScreenShareMessage>();
        ScreenShareSender announcedSender = new ScreenShareSender(
            local, "Main Screen", source, announcedMessages.Add, options, timeProvider);

        announcedSender.CaptureOnce();
        announcedSender.Stop();

        Assert.IsType<StreamStopped>(announcedMessages[^1]);
    }

    [Fact]
    public void Dispose_DisposesDisposableSourceAndHandlesNonDisposableSource()
    {
        Participant local = CreateLocalParticipant();
        ScreenShareOptions options = CreateDefaultOptions();
        ManualTimeProvider timeProvider = new ManualTimeProvider();

        DisposableFakeImageSource disposableSource = new DisposableFakeImageSource();
        ScreenShareSender senderWithDisposable = new ScreenShareSender(
            local, "Main Screen", disposableSource, _ => { }, options, timeProvider);

        senderWithDisposable.StartLoop();
        senderWithDisposable.Dispose();

        Assert.True(disposableSource.IsDisposed);
        Assert.False(senderWithDisposable.CaptureOnce());

        FakeImageSource nonDisposableSource = new FakeImageSource();
        ScreenShareSender senderWithoutDisposable = new ScreenShareSender(
            local, "Main Screen", nonDisposableSource, _ => { }, options, timeProvider);

        senderWithoutDisposable.Dispose();
        Assert.False(senderWithoutDisposable.CaptureOnce());
    }

    private class FakeImageSource : IImageSource
    {
        public Exception? ExceptionToThrow { get; set; }

        public Bitmap Capture()
        {
            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            return new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        }
    }

    private sealed class DisposableFakeImageSource : FakeImageSource, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void Advance(TimeSpan delta)
        {
            _utcNow = _utcNow.Add(delta);
        }
    }
}
