namespace ScreenShare;

public enum ScreenShareMessageKind : byte
{
    Data = 1,
    StreamStarted = 2,
    StreamStopped = 3,
    Subscribe = 4,
    Unsubscribe = 5,
    RequestFullFrame = 6,
    FrameAck = 7,
    UpdateSendToList = 8,
    Hello = 9,



}
