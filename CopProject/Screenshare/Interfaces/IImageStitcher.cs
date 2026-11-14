using System.Drawing;

namespace ScreenShare;

/// <summary>
/// rebuilds a stream frame on the receiver. the sender diffs every frame against
/// the one before it, so deltas and pulses are only accepted in strict frame order.
/// </summary>
public interface IImageStitcher
{
    ///<summary>
    /// frame number of the last accepted packet, -1 if nothing has been accepted. helps maintain sync between packet recieved is outdated or new
    ///</summary>
    long LastFrameNumber { get; }

    ///<summary>
    /// true until a full frame arrives, and again after a gap in frame numbers.
    /// while set, deltas and pulses are dropped.
    ///</summary>
    bool NeedsFullFrame { get; }

    ///<summary>
    /// stores complete screen image. for first screen capture or when sender decides to send full frame.
    /// accepted when frameNumber is newer than LastFrameNumber.
    ///</summary>
    /// <returns>true if the frame was applied, false if it was stale</returns>
    bool ApplyFullFrame(long frameNumber, Bitmap frame);

    ///<summary>
    /// updates the regions of the frame covered by the given tiles, all belonging to one frame.
    /// accepted only when frameNumber is exactly LastFrameNumber + 1.
    ///</summary>
    /// <returns>true if the tiles were applied, false if the packet was stale or followed a gap</returns>
    bool ApplyChangedTiles(long frameNumber, IReadOnlyList<Tile> tiles);

    ///<summary>
    /// records that the screen did not change. accepted only when frameNumber is exactly LastFrameNumber + 1.
    ///</summary>
    /// <returns>true if the pulse was accepted, false if it was stale or followed a gap</returns>
    bool ApplyPulse(long frameNumber);

    ///<summary>
    /// returns the new constructed frame or null if not frame is recieved
    ///</summary>
    Bitmap? GetCurrentFrame();

    ///<summary>
    /// for cleaning at end of stream or restart
    ///</summary>
    void Reset();
}
