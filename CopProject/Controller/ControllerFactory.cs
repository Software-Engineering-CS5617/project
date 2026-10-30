using System.Net;
using Networking;

namespace Controller;

// Factory for creating the correct IControllerService implementation.
// callers depend on IControllerService, not on HostController or ClientController directly.

public static class ControllerFactory
{
    // Creates a host-side controller that can create, start, and end meetings
    public static IControllerService CreateHost(string hostUserId)
    {
        ICommunicator communicator = CommunicatorFactory.CreateCommunicator(null, isServer: true, isClient: false)
            ?? throw new InvalidOperationException("Failed to create host communicator.");
        return new HostController(hostUserId, communicator);
    }

    // Creates a client-side controller that can join and leave remote meetings
    public static IControllerService CreateClient(string userId)
    {
        return new ClientController(userId);
    }
}
