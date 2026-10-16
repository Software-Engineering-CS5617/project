using System.Net;

namespace Networking;

/// <summary>
/// Declares a communicator that can send and receive messages.
/// </summary>
public interface ICommunicator
{
    /// <summary>
    /// Gets the port that is used for listening.
    /// </summary>
    int ListenPort { get; }

    /// <summary>
    /// Adds a subscriber.
    /// </summary>
    /// <param name="module">module id for the subscriber</param>
    /// <param name="subscriber">The message listener instance</param>
    void AddSubscriber(int module, IMessageListener subscriber);

    /// <summary>
    /// Removes a subscriber
    /// </summary>
    /// <param name="module">Module id of the subscriber to be removed</param>
    void RemoveSubscriber(int module);

    /// <summary>
    /// Sends the given message to the given ip and port.
    /// </summary>
    /// <param name="ipEndPoint">IP EndPoint(Address:Port) of the destination</param>
    /// <param name="message">Message to be sent</param>
    /// <param name="module">Module id of the module to which the message should be sent</param>
    /// <param name="priority">Priority of the message to be sent</param>
    void SendMessage(IPEndPoint iPEndPoint, byte[] message, int module, int priority);


    /// <summary>
    /// Broadcasts the message to every listener in a given module
    /// </summary>
    /// <param name="message">Message to be broadcasted</param>
    /// <param name="module">Module ID of the module to which the message should be broadcasted</param>
    /// <param name="priority">Priority of the message</param>
    void BroadcastMessage(byte[] message, int module, int priority);

    ///Multicast Methods

    /// <summary>
    /// Join a Multicast Group
    /// </summary>
    /// <param name="multicastGroup">IP Endpoint of the joining Multicast group</param>
    void JoinMulticastGroup(int multicastGroup);

    /// <summary>
    /// Leave a Multicast group
    /// </summary>
    /// <param name="multicastGroup">IP Endpoint of the leaving Mutlicast group</param>
    void LeaveMulticastGroup(int multicastGroup);


    /// <summary>
    /// Send message to all users in a Multicast group
    /// </summary>
    /// <param name="multicastGroup">IP Endpoint of the Mutlicast group</param>
    /// <param name="message">Message to be sent</param>
    /// <param name="module">Module ID of the module to which the message is multicasted</param>
    /// <param name="priority">Priority of the message</param>
    void MulticastMessage(int multicastGroup, byte[] message, int module, int priority);


}