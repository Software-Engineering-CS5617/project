namespace ScreenShare;


/// <summary>
/// sent by sharer to host and forwareded to viewer when stream stops
/// </summary>
public sealed record StreamStopped : IScreenShareMessage
{
    public string StreamId { get; }
    public ScreenShareMessageKind Kind => ScreenShareMessageKind.StreamStopped;

    /// <summary>
    /// <excpetion cref="ArgumentException">stream id is empty</excpetion>
    /// </summary>
    /// <param name="streamId">gets id of stream that has stopped</param>
    public StreamStopped(string streamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId, nameof(streamId));
        StreamId = streamId;
    }
}
