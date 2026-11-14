namespace ScreenShare;

/// <summary>
/// sent  by the viewer to start recieving a stream
/// </summary>
public sealed record Subscribe : IScreenShareMessage
{
    public string StreamId { get; }
    public Participant Viewer { get; }
    public int MaxHeight { get; }

    public ScreenShareMessageKind Kind => ScreenShareMessageKind.Subscribe;


    /// <summary>
    /// <exception cref="ArgumentException"> streamId is null or whitespace</exception>
    /// <exception cref="ArgumentNullException">viewer is null </exception> viewer is null
    /// <exception cref="ArgumentOutOfRangeException"> dimentions are invalid</exception>
    /// </summary>
    /// <param name="streamId">gets the id of requested stream</param>
    /// <param name="viewer">get the viewer asking for stream</param>
    /// <param name="maxHeight">gets the largest frame height viewer wants</param>
    public Subscribe(Participant viewer, string streamId, int maxHeight)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId, nameof(streamId));
        ArgumentNullException.ThrowIfNull(viewer, nameof(viewer));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHeight, nameof(maxHeight));
        StreamId = streamId;
        Viewer = viewer;
        MaxHeight = maxHeight;
    }


}
