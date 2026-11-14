namespace ScreenShare;

public sealed record FrameAck : IScreenShareMessage
{
    public Participant Viewer { get; }
    public string StreamId { get; }
    public long FrameNumber { get; }
    public ScreenShareMessageKind Kind => ScreenShareMessageKind.FrameAck;

    public FrameAck(Participant viewer, string streamId, long frameNumber)
    {
        ArgumentNullException.ThrowIfNull(viewer, nameof(viewer));
        ArgumentNullException.ThrowIfNullOrWhiteSpace(streamId, nameof(streamId));
        ArgumentOutOfRangeException.ThrowIfNegative(frameNumber, nameof(frameNumber));

        Viewer = viewer;
        StreamId = streamId;
        FrameNumber = frameNumber;


    }

}
