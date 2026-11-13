namespace ScreenShare;


/// <summary>
/// sent by viewer to host to stop recieiving a stream. e.g. when tile goes off screen when scrolled
/// </summary>
public sealed record Unsubscribe : IScreenShareMessage
{

    public Participant Viewer { get; }
    public string StreamId { get; }
    public ScreenShareMessageKind Kind => ScreenShareMessageKind.Unsubscribe;


    /// <summary>
    /// <exception cref="ArgumentNullException">viewr is NULL</exception>
    /// <exception cref="ArgumentException"> streamId is NULL </exception>
    /// </summary>
    /// <param name="viewer"> gets the viewer dropping the stream</param>
    /// <param name="streamId">gets the id of stream to drop</param>
    public Unsubscribe(Participant viewer, string streamId)
    {
        ArgumentNullException.ThrowIfNull(viewer, nameof(viewer));
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId, nameof(streamId));

        Viewer = viewer;
        StreamId = streamId;
    }


}
