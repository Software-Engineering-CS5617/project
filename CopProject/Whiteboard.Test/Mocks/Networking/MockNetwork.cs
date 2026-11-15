/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace Whiteboard.Test.Mocks.Networking;

/// <summary>
/// Mocks a LAN network
/// </summary>
public class MockNetwork
{
    /// <summary>
    /// Communicators of all mock device connected to the network
    /// </summary>
    private readonly List<MockCommunicator> _deviceCommunicators = [];

    /// <summary>
    /// Add a mock device communicator to the network
    /// </summary>
    /// <param name="communicator">Communicator to add</param>
    public void AddCommunicator(MockCommunicator communicator)
    {
        _deviceCommunicators.Add(communicator);
    }

    /// <summary>
    /// Remove a mock device communicator from the network
    /// </summary>
    /// <param name="communicator">Communicator to remove</param>
    public void RemoveDevice(MockCommunicator communicator)
    {
        _deviceCommunicators.Remove(communicator);
    }

    /// <summary>
    /// Send a message to the mock device with port given by <paramref name="endPoint"/>
    /// </summary>
    /// <param name="endPoint">Destination IP endpoint</param>
    /// <param name="message">Message to send</param>
    public void SendTo(IPEndPoint endPoint, byte[] message)
    {
        MockCommunicator? targetCommunicator = _deviceCommunicators.Find(c => c.ListenPort == endPoint.Port);

        targetCommunicator?.InvokeSubscriber(message);
    }

    /// <summary>
    /// Send a message to all mock devices
    /// </summary>
    /// <param name="message">Message to send</param>
    /// <param name="sender">Sender communicator</param>
    public void SendAll(MockCommunicator sender, byte[] message)
    {
        foreach (MockCommunicator communicator in _deviceCommunicators.ToList())
        {
            if (communicator != sender)
            {
                communicator.InvokeSubscriber(message);
            }
        }
    }
}
