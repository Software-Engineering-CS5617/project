namespace ScreenShare;

/// <summary>
/// screenshare disconnect detection and recovery
/// </summary>
public sealed record ResilienceOptions
{
    /// <summary>
    /// how long host waits for peer traffic before declaring it gone
    /// Should be more than ScreenShareOptions.AckTimeout since traffic can be slow
    /// </summary>
    public TimeSpan PeerSilenceTimeout { get; init; } = TimeSpan.FromSeconds(8);

    /// <summary>
    /// how long client waits for host traffic before declaring it gone
    /// Should be more than PeerSilenceTimeout, host shouldn't be declared gone when actually another peer is gone
    /// </summary>
    public TimeSpan HostSilenceTimeout { get; init; } = TimeSpan.FromSeconds(12);

    /// <summary>how long a subscribed stream may deliver nothing before the client should resubscribe</summary>
    public TimeSpan StreamStallTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>gap between recovery attempts</summary>
    public TimeSpan RecoveryInterval { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>how often session checks for silent peers and stalled streams</summary>
    public TimeSpan SweepInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>how many times a stalled stream is resubscribed before the client gives up on it</summary>
    public int MaxRecoveryAttempts { get; init; } = 6;

    /// <summary>
    /// how long after a reconnect the client waits for the host to re-announce its streams
    /// streams that stay unannounced ended while the client was offline, to figure this out
    /// </summary>
    public TimeSpan ReconcileWindow { get; init; } = TimeSpan.FromSeconds(3);

    /// <exception cref="ArgumentOutOfRangeException">a value is zero or negative</exception>
    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(PeerSilenceTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(HostSilenceTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(StreamStallTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(RecoveryInterval, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(SweepInterval, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ReconcileWindow, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxRecoveryAttempts);
    }
}
