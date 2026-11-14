namespace ScreenShare;

/// <summary>
/// tuning for capture, flow control and cleanup
/// </summary>
public sealed record ScreenShareOptions
{
    /// <summary>gets how many frames per second the sender tries to capture (spec: 24 to 30)</summary>
    public int FramesPerSecond { get; init; } = 50;

    /// <summary>gets the height captures are scaled down to; wider aspect ratios keep their shape</summary>
    public int MaxHeight { get; init; } = 480;

    /// <summary>gets how many frames the sender may send before the host acknowledges them</summary>
    public int MaxFramesInFlight { get; init; } = 8;

    /// <summary>
    /// gets how long the sender waits for an acknowledgement before assuming the frames
    /// were lost and starting over with a full frame
    /// </summary>
    public TimeSpan AckTimeout { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// gets how many unacknowledged frames a viewer may have before the host stops sending it
    /// deltas; once it catches up it gets one full frame instead
    /// </summary>
    public int MaxViewerLag { get; init; } = 5;

    /// <summary>
    /// gets how long a viewer may leave frames unacknowledged before the host drops it,
    /// which catches clients that crashed without leaving the meeting
    /// </summary>
    public TimeSpan ViewerTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>gets how often the host checks for timed-out viewers</summary>
    public TimeSpan MaintenanceInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <exception cref="ArgumentOutOfRangeException">a value is zero or negative</exception>
    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(FramesPerSecond);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxFramesInFlight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxViewerLag);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(AckTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ViewerTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(MaintenanceInterval, TimeSpan.Zero);
    }
}
