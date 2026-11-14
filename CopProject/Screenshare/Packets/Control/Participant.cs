using System.Net;

namespace ScreenShare;

/// <summary>
/// Identifies who sent a control message. Sender includes their own ip and endpoint
/// </summary>
public sealed record Participant
{
    public string Id { get; } // Participant ID
    public IPEndPoint Endpoint { get; } // gets the endpoint host uses to reach this participant

    /// <summary
    /// <exception cref="ArgumentException"> the id is empty</exception>
    /// <exception cref="ArgumentNullException"> IPEndpoint is NULL</exception>
    /// </summary>
    /// <param name="id"></param>
    /// <param name="endpoint"></param>
    public Participant(string id, IPEndPoint endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
        ArgumentNullException.ThrowIfNull(endpoint, nameof(endpoint));

        Id = id;
        Endpoint = endpoint;
    }

}
