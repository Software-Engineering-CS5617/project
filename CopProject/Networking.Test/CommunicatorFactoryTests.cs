using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking.TCPCommunication;

namespace Networking.Test;

/// <summary>
/// Unit tests for CommunicatorFactory.
/// </summary>
[TestClass]
public class CommunicatorFactoryTests
{
    /// <summary>
    /// Checks that the factory creates a CommunicatorServer instance.
    /// </summary>
    [TestMethod]
    public void CreateCommunicator_ServerMode_ReturnsServerInstance()
    {
        using CommunicatorServer? server = CommunicatorFactory.CreateCommunicator(null, isServer: true, isClient: false) as CommunicatorServer;

        Assert.IsNotNull(server);
        Assert.IsGreaterThan(0, server.ListenPort);
        Assert.IsNotNull(server.ListenIpAddress);
        Assert.AreNotEqual(IPAddress.Any, server.ListenIpAddress);
    }

    /// <summary>
    /// Checks that the factory creates a CommunicatorClient instance when given a server endpoint.
    /// </summary>
    [TestMethod]
    public void CreateCommunicator_ClientModeWithServerIp_ReturnsClientInstance()
    {
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, 9811);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient? client = CommunicatorFactory.CreateCommunicator(serverEndpoint, isServer: false, isClient: true) as CommunicatorClient;

        Assert.IsNotNull(client);
        Assert.IsTrue(IPAddress.IsLoopback(client.ListenIpAddress));
    }

    /// <summary>
    /// Checks that client creation returns null when no server IP is provided.
    /// </summary>
    [TestMethod]
    public void CreateCommunicator_ClientModeWithoutServerIp_ReturnsNull()
    {
        ICommunicator? communicator = CommunicatorFactory.CreateCommunicator(null, isServer: false, isClient: true);

        Assert.IsNull(communicator);
    }

    /// <summary>
    /// Checks that ambiguous boolean flags return null.
    /// </summary>
    [TestMethod]
    public void CreateCommunicator_AmbiguousFlags_ReturnsNull()
    {
        ICommunicator? bothTrue = CommunicatorFactory.CreateCommunicator(null, isServer: true, isClient: true);
        ICommunicator? bothFalse = CommunicatorFactory.CreateCommunicator(null, isServer: false, isClient: false);

        Assert.IsNull(bothTrue);
        Assert.IsNull(bothFalse);
    }
}
