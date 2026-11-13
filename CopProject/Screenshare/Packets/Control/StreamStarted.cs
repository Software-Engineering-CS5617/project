namespace ScreenShare;


/// <summary>
/// Sent by a sharer to the host, and forwared to viewrs, when stream starts
/// </summary>
public sealed record StreamStarted : IScreenShareMessage
{
    public string StreamId { get; }
    public Participant Sharer { get; }
    public string DisplayName { get; }
    public int Width { get; }
    public int Height { get; }

    public ScreenShareMessageKind Kind => ScreenShareMessageKind.StreamStarted;

    /// <summary>
    /// <exception cref="ArgumentException">id/name is empry or dimentions are not valid</exception>
    /// <exception cref="ArgumentNullException">sharer is null</exception>
    /// </summary>
    /// <param name="streamId">gets the id of stream that started</param>
    /// <param name="sharer">gets the participant sharing the stream</param>
    /// <param name="displayName"> gets the name shown on display</param>
    /// <param name="width">gets the width of shared screen in pixels</param>
    /// <param name="height">gets the height of shared screen in pixels</param>
    public StreamStarted(string streamId, Participant sharer, string displayName, int width, int height)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId, nameof(streamId));
        ArgumentNullException.ThrowIfNull(sharer, nameof(sharer));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height, nameof(height));

        StreamId = streamId;
        Sharer = sharer;
        DisplayName = displayName;
        Width = width;
        Height = height;

    }

}
