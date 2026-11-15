/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using System.Net;
using Networking;
using Whiteboard.Test.Mocks.Networking;

namespace Whiteboard.Test.ClientServer;

/// <summary>
/// Tests that passing null or empty string to whiteboard factory throws appropriate exceptions
/// </summary>
public class InitTests
{
    private readonly ICommunicator _clientCommunicator = new MockCommunicatorFactory().CreateClientCommunicator();
    private readonly IPEndPoint _endPoint = new(IPAddress.Loopback, 1000);
    private readonly string _userId = "user1";

    [Fact]
    public void NullClientCommunicatorThrows()
    {
        Assert.Throws<ArgumentNullException>(
            () => WhiteboardFactory.CreateClientWhiteboard(null, _endPoint, _userId)
        );
    }


    [Fact]
    public void NullIPEndPointThrows()
    {
        ICommunicator communicator = new MockCommunicatorFactory().CreateClientCommunicator();

        Assert.Throws<ArgumentNullException>(
            () => WhiteboardFactory.CreateClientWhiteboard(communicator, null, _userId)
        );
    }

    [Fact]
    public void NullOrEmptyUserIdThrows()
    {
        ICommunicator communicator = new MockCommunicatorFactory().CreateClientCommunicator();

        Assert.Throws<ArgumentException>(
            () => WhiteboardFactory.CreateClientWhiteboard(communicator, _endPoint, "")
        );

        Assert.Throws<ArgumentNullException>(
            () => WhiteboardFactory.CreateClientWhiteboard(communicator, _endPoint, null)
        );
    }

    [Fact]
    public void NullServerCommunicatorThrows()
    {
        Assert.Throws<ArgumentNullException>(
            () => WhiteboardFactory.GetServerWhiteboard(null)
        );
    }
}
