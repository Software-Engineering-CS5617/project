using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking.TCPCommunication;

namespace Networking.Test;

/// <summary>
/// Unit tests for CommunicatorFactory.
/// </summary>
#pragma warning disable CS0618
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
#pragma warning disable CS0618
        ICommunicator? bothTrue = CommunicatorFactory.CreateCommunicator(null, isServer: true, isClient: true);
        ICommunicator? bothFalse = CommunicatorFactory.CreateCommunicator(null, isServer: false, isClient: false);
#pragma warning restore CS0618

        Assert.IsNull(bothTrue);
        Assert.IsNull(bothFalse);
    }

    /// <summary>
    /// Checks that CreateServerCommunicator creates a CommunicatorServer instance.
    /// </summary>
    [TestMethod]
    public void CreateServerCommunicator_ReturnsServerInstance()
    {
        using CommunicatorServer? server = CommunicatorFactory.CreateServerCommunicator() as CommunicatorServer;

        Assert.IsNotNull(server);
        Assert.IsGreaterThan(0, server.ListenPort);
        Assert.IsNotNull(server.ListenIpAddress);
        Assert.AreNotEqual(IPAddress.Any, server.ListenIpAddress);
    }

    /// <summary>
    /// Checks that CreateServerCommunicator with specific endpoint returns configured server.
    /// </summary>
    [TestMethod]
    public void CreateServerCommunicator_WithEndpoint_ReturnsServerInstance()
    {
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, 9812);
        using CommunicatorServer? server = CommunicatorFactory.CreateServerCommunicator(serverEndpoint) as CommunicatorServer;

        Assert.IsNotNull(server);
        Assert.AreEqual(9812, server.ListenPort);
    }

    /// <summary>
    /// Checks that CreateClientCommunicator creates a CommunicatorClient connected to the server.
    /// </summary>
    [TestMethod]
    public void CreateClientCommunicator_WithServerIp_ReturnsClientInstance()
    {
        IPEndPoint serverEndpoint = new(IPAddress.Loopback, 9813);
        using CommunicatorServer server = new(serverEndpoint);

        using CommunicatorClient? client = CommunicatorFactory.CreateClientCommunicator(serverEndpoint) as CommunicatorClient;

        Assert.IsNotNull(client);
        Assert.IsTrue(IPAddress.IsLoopback(client.ListenIpAddress));
    }
}
