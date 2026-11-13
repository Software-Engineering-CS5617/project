using ScreenShare.Interfaces;

namespace ScreenShare;

/// <summary>
/// the part of the ScreenShare view the meeting integration drives
/// </summary>
internal interface IScreenShareView
{
    /// <summary>shows a new meeting's streams</summary>
    void Show(IScreenShareFeed feed);

    /// <summary>empties the view when the meeting ends</summary>
    void Clear();
}
