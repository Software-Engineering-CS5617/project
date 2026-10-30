using Cloud.Networking;

namespace Cloud.Test;

/// <summary>
/// Tests the Cloud.Networking module.
/// </summary>
[TestClass]
public class TestNetworking
{
    /// <summary>
    /// Tests whether the client can successfully connect to the server.
    /// </summary>
    [TestMethod]
    public async Task TestServerConnection()
    {
        // TODO: ADD THE RIGHT URL LATER (works with right url)
        string url = "https://{wrong-url-must-fail}/hub";

        IMessenger messenger = new SignalRMessenger(url);

        try
        {
            await messenger.ConnectAsync();
        }
        finally
        {
            await messenger.DisconnectAsync();
        }
    }
}
