using System.Net;
using Networking;
using Utils;
namespace Controller;

// Factory for creating the correct IControllerService implementation.
// callers depend on IControllerService, not on HostController or ClientController directly.

public static class ControllerFactory
{
    // Creates a host-side controller that can create, start, and end meetings
    public static IControllerService CreateHost(ControllerUser hostUser)
    {
        hostUser.IsHost = true;
        ICommunicator communicator = CommunicatorFactory.CreateCommunicator(null, isServer: true, isClient: false)
            ?? throw new InvalidOperationException("Failed to create host communicator.");
        return new HostController(hostUser, communicator);
    }
    public static IControllerService CreateHost(string username, string gmail = "", byte[]? profilePicture = null)
    {
        var hostUser = new ControllerUser(username, gmail, profilePicture, string.Empty, 0, isHost: true);
        return CreateHost(hostUser);
    }
    // Creates a client-side controller that can join and leave remote meetings
    public static IControllerService CreateClient(ControllerUser clientUser)
    {
        clientUser.IsHost = false;
        return new ClientController(clientUser);
    }
    public static IControllerService CreateClient(string username, string gmail = "", byte[]? profilePicture = null)
    {
        var clientUser = new ControllerUser(username, gmail, profilePicture, string.Empty, 0, isHost: false);
        return CreateClient(clientUser);
    }
}
