namespace Cloud.Networking;

/// <summary>
/// Defines the operations available to clients.
/// </summary>
public interface IMessenger
{
    /// <summary>
    /// Connects to the server.
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    /// Sends a message to the server.
    /// </summary>
    Task SendMessageAsync(string message);

    /// <summary>
    /// Disconnects from the server.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Notify the subscribers when a message is received.
    /// </summary>
    event Action<string>? MessageReceived;
}
