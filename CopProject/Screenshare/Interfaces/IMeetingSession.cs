using System.Net;
using Networking;

namespace ScreenShare;

/// <summary>
/// what ScreenShare needs from the meeting controler. ScreenShare doesn't track 
/// membership itself; an adapter over Controller module must implement this
/// As of no controller module is no not implemented yet hence adapter needs
/// to be discussed
/// </summary>
public interface IMeetingSession
{
    /// <summary>
    /// gets the participant running this instance
    /// </summary>
    MeetingParticipant LocalUser { get; }


    /// <summary>
    /// gets the host's listing endinpoint; client will send everything here
    /// </summary>
    IPEndPoint HostEndpoint { get; }

    /// <summary>
    /// gets the meetings shared connection, ScreenShare subscribes to it under it's own module it 
    /// bue newer creates or disposes it.
    /// </summary>
    ICommunicator Communicator { get; }

    /// <summary>
    /// Raised on host when a participant leaves, so their stream and subscription are dropped
    /// </summary>
    event Action<MeetingParticipant>? ParticipantLeft;

    /// <summary>
    /// raised when the meeting ends or this participant leaves it
    /// </summary>
    event Action? MeetingEnded;



}
