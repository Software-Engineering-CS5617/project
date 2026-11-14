using ScreenShare.Models;

namespace ScreenShare.Interfaces;

/// <summary>
/// what the ScreenShare UI needs: who is sharing, their pictures, and the share toggle.
/// events may be raised on any thread.
/// </summary>
public interface IScreenShareFeed
{
    event Action<string, string>? ParticipantJoined; // id, name
    event Action<string>? ParticipantLeft;  // id
    event Action<string, ScreenFrame>? FrameReady; // id, raw pixels of the latest picture

    void RequestFeeds(IReadOnlyDictionary<string, int> idToResolution); // who is visible at what quality

    /// <summary>gets whether this participant is sharing their screen</summary>
    bool IsSharing { get; }

    /// <summary>starts sharing this participant's screen</summary>
    void StartSharing();

    /// <summary>stops sharing this participant's screen</summary>
    void StopSharing();
}
