namespace Cloud.Networking;

/// <summary>
/// Hub method names shared by the client and the server.
/// </summary>
public static class Contract
{
    public const string SendMessage = nameof(SendMessage);
    public const string ReceiveMessage = nameof(ReceiveMessage);
}
