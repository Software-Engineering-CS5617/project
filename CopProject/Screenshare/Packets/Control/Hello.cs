namespace ScreenShare;

/// <summary>
/// sent by a client to the host once its ScreenShare is listening. the host replies with
/// a StreamStarted for every running stream, so late joiners see existing shares.
/// </summary>
public sealed record Hello : IScreenShareMessage
{
    public Participant Viewer { get; }

    public ScreenShareMessageKind Kind => ScreenShareMessageKind.Hello;

    /// <summary>
    ///  <exception cref="ArgumentNullException"> viewer is NULL</exception>
    /// </summary>
    /// <param name="viewer">Client Introducint himself</param>
    public Hello(Participant viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        Viewer = viewer;
    }
}
