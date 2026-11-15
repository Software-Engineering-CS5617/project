/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using Networking;

namespace Whiteboard.Test.Mocks.Networking;

/// <summary>
/// Factory for mock communicators
/// </summary>
public class MockCommunicatorFactory
{
    /// <summary>
    /// Singleton mock server communicator
    /// </summary>
    private readonly MockCommunicator _serverCommunicator;

    /// <summary>
    /// Singleton mock network
    /// </summary>
    private readonly MockNetwork _mockNetwork = new();

    /// <summary>
    /// Next mock port for allocation
    /// </summary>
    private int _nextPort;

    /// <summary>
    /// Mock IP endpoint for the server
    /// </summary>
    public IPEndPoint ServerEndPoint { get; private set; }

    public MockCommunicatorFactory()
    {
        int serverPort = _nextPort++;

        _serverCommunicator = new MockCommunicator(_mockNetwork, serverPort);
        _mockNetwork.AddCommunicator(_serverCommunicator);
        ServerEndPoint = new IPEndPoint(IPAddress.Loopback, serverPort);
    }

    /// <summary>
    /// Get the server communicator
    /// </summary>
    /// <returns>The server communicator</returns>
    public ICommunicator GetServerCommunicator()
    {
        return _serverCommunicator;
    }

    /// <summary>
    /// Create a new client communicator
    /// </summary>
    /// <returns>A new client communicator</returns>
    public ICommunicator CreateClientCommunicator()
    {
        MockCommunicator clientCommunicator = new MockCommunicator(_mockNetwork, _nextPort++);
        _mockNetwork.AddCommunicator(clientCommunicator);

        return clientCommunicator;
    }
}
