
using ScreenShare;


/// <summary>
/// sent by viewer, throught host to a sharer when viewer missed a frame or joined mid-stream and 
/// can not apply deltas until it has full frame
/// </summary>
public sealed record RequestFullFrame : IScreenShareMessage
{
    public Participant Viewer { get; }
    public string StreamId { get; }

    public ScreenShareMessageKind Kind => ScreenShareMessageKind.RequestFullFrame;

    /// <summary>
    /// <exception cref="ArgumentNullException"> Participant is NULL</exception>
    /// <exception cref="ArgumentException">StreamId is null or whitespace</exception>
    /// </summary>
    /// <param name="viewer"></param>
    /// <param name="streamId"></param>
    public RequestFullFrame(Participant viewer, string streamId)
    {
        ArgumentNullException.ThrowIfNull(viewer, nameof(viewer));
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId, nameof(streamId));

        Viewer = viewer;
        StreamId = streamId;

    }
}
