using System.Net;

namespace ScreenShare;


/// <summary>
/// sent by the host to a sharer with the full list of wiewers to send the strem to. 
/// reserved for peer to peer model, may not be useful now
/// </summary>
public sealed record UpdateSendToList : IScreenShareMessage
{
    public string StreamId { get; }
    public IReadOnlyList<IPEndPoint> SendTo { get; }

    public ScreenShareMessageKind Kind => ScreenShareMessageKind.UpdateSendToList;


    /// <summary
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="sendTo"></param>
    /// <exception cref="ArgumentException"></exception>
    public UpdateSendToList(string streamId, IReadOnlyList<IPEndPoint> sendTo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId, nameof(streamId));
        ArgumentNullException.ThrowIfNull(sendTo, nameof(sendTo));

        // Check if any endpoint int the list is null
        if (sendTo.Any(endpoint => endpoint == null))
        {
            throw new ArgumentException("the sent-to list can not contain null", nameof(sendTo));
        }

        StreamId = streamId;
        SendTo = Array.AsReadOnly(sendTo.ToArray());
    }
}
