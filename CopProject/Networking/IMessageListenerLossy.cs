namespace Networking;

/// <summary>
/// Declares a listener for lossy (UDP) messages from the communicator.
/// </summary>
public interface IMessageListenerLossy
{
    /// <summary>
    /// Handles reception of a lossy message.
    /// </summary>
    /// <param name="message">Message data that is received</param>
    void OnMessageReceivedLossy(byte[] message);
}
