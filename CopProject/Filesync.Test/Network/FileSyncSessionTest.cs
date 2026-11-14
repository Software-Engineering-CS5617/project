// Author : Murali

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FileSynchroniser.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking;
using Utils;

namespace Filesync.Test.Network;

[TestClass]
public class FileSyncSessionTest
{
    private static readonly IPEndPoint s_hostEp = new(IPAddress.Loopback, 8001);
    private static readonly IPEndPoint s_clientEp = new(IPAddress.Loopback, 8002);

    private FakeCommunicator _communicator = null!;

    /// <summary>
    /// Minimal communicator: the session only carries it, it never calls it
    /// </summary>
    private sealed class FakeCommunicator : ICommunicator
    {
        public int ListenPort => 8001;
        public IPAddress ListenIpAddress => IPAddress.Loopback;

        public void AddSubscriber(int module, IMessageListener subscriber) { }
        public void RemoveSubscriber(int module) { }
        public void SendMessage(IPEndPoint ipEndPoint, byte[] message, int module, int priority) { }
        public void BroadcastMessage(byte[] message, int module, int priority) { }
        public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority) { }
        public void JoinMulticastGroup(int multicastGroup, IPEndPoint clientIp) { }
        public void LeaveMulticastGroup(int multicastGroup, IPEndPoint clientIp) { }
        public void AddLossySubscriber(int module, IMessageListenerLossy subscriber) { }
        public void RemoveLossySubscriber(int module) { }
        public void SendMessageLossy(IPEndPoint ipEndPoint, byte[] message, int module, int priority) { }
        public void BroadcastMessageLossy(byte[] message, int module, int priority) { }
        public void MulticastMessageLossy(int multicastGroup, byte[] message, int module, int priority) { }
    }

    [TestInitialize]
    public void Setup()
    {
        _communicator = new FakeCommunicator();
    }

    private static ControllerUser MakeUser(string name, int port)
    {
        return new ControllerUser(name, $"{name}@smail.iitpkd.ac.in", null, "127.0.0.1", port, isHost: false);
    }

    /// <summary>
    /// 1. The required values are stored as given
    /// </summary>
    [TestMethod]
    public void Constructor_StoresRequiredValues()
    {
        var session = new FileSyncSession(_communicator, true, s_hostEp, null);

        Assert.AreSame(_communicator, session.Communicator);
        Assert.IsTrue(session.IsHost);
        Assert.AreEqual(s_hostEp, session.LocalEndpoint);
    }

    /// <summary>
    /// 2. Peers and the register / unregister callbacks are optional and default to null
    /// </summary>
    [TestMethod]
    public void Constructor_OptionalParameters_DefaultToNull()
    {
        var session = new FileSyncSession(_communicator, true, s_hostEp, null);

        Assert.IsNull(session.InitialPeers);
        Assert.IsNull(session.RegisterHandler);
        Assert.IsNull(session.UnregisterHandler);
    }

    /// <summary>
    /// 3. A host session has no host endpoint
    /// </summary>
    [TestMethod]
    public void HostSession_HasNoHostEndpoint()
    {
        var session = new FileSyncSession(_communicator, true, s_hostEp, null);

        Assert.IsTrue(session.IsHost);
        Assert.IsNull(session.HostEndpoint);
    }

    /// <summary>
    /// 4. A client session keeps the host endpoint next to its own endpoint
    /// </summary>
    [TestMethod]
    public void ClientSession_KeepsHostEndpoint()
    {
        var session = new FileSyncSession(_communicator, false, s_clientEp, s_hostEp);

        Assert.IsFalse(session.IsHost);
        Assert.AreEqual(s_clientEp, session.LocalEndpoint);
        Assert.AreEqual(s_hostEp, session.HostEndpoint);
    }

    /// <summary>
    /// 5. The initial peers are kept in order
    /// </summary>
    [TestMethod]
    public void Constructor_InitialPeers_AreKeptInOrder()
    {
        ControllerUser first = MakeUser("alice", 8002);
        ControllerUser second = MakeUser("bob", 8003);
        var peers = new List<ControllerUser> { first, second };

        var session = new FileSyncSession(_communicator, true, s_hostEp, null, peers);

        Assert.IsNotNull(session.InitialPeers);
        List<ControllerUser> stored = session.InitialPeers.ToList();
        Assert.HasCount(2, stored);
        Assert.AreSame(first, stored[0]);
        Assert.AreSame(second, stored[1]);
    }

    /// <summary>
    /// 6. An empty peer list is kept as an empty list, not turned into null
    /// </summary>
    [TestMethod]
    public void Constructor_EmptyPeers_StayEmpty()
    {
        var session = new FileSyncSession(_communicator, true, s_hostEp, null, new List<ControllerUser>());

        Assert.IsNotNull(session.InitialPeers);
        Assert.HasCount(0, session.InitialPeers.ToList());
    }

    /// <summary>
    /// 7. The register and unregister callbacks are the same delegates and can be called
    /// </summary>
    [TestMethod]
    public void Constructor_Callbacks_AreStoredAndInvocable()
    {
        int registered = 0;
        int unregistered = 0;
        Action<IUserNotificationHandler> register = _ => registered++;
        Action<IUserNotificationHandler> unregister = _ => unregistered++;

        var session = new FileSyncSession(_communicator, true, s_hostEp, null, null, register, unregister);

        Assert.AreSame(register, session.RegisterHandler);
        Assert.AreSame(unregister, session.UnregisterHandler);

        session.RegisterHandler!.Invoke(null!);
        session.UnregisterHandler!.Invoke(null!);
        session.UnregisterHandler.Invoke(null!);

        Assert.AreEqual(1, registered);
        Assert.AreEqual(2, unregistered);
    }

    /// <summary>
    /// 8. Sessions with the same values are equal and share a hash code
    /// </summary>
    [TestMethod]
    public void Equality_SameValues_AreEqual()
    {
        var a = new FileSyncSession(_communicator, false, s_clientEp, s_hostEp);
        var b = new FileSyncSession(_communicator, false, s_clientEp, s_hostEp);

        Assert.AreEqual(a, b);
        Assert.IsTrue(a == b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// 9. Endpoints are compared by value, not by instance
    /// </summary>
    [TestMethod]
    public void Equality_EndpointsWithSameAddressAndPort_AreEqual()
    {
        var a = new FileSyncSession(_communicator, false, new IPEndPoint(IPAddress.Loopback, 8002), new IPEndPoint(IPAddress.Loopback, 8001));
        var b = new FileSyncSession(_communicator, false, new IPEndPoint(IPAddress.Loopback, 8002), new IPEndPoint(IPAddress.Loopback, 8001));

        Assert.AreEqual(a, b);
    }

    /// <summary>
    /// 10. Sessions differing in any value are not equal
    /// </summary>
    [TestMethod]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var baseline = new FileSyncSession(_communicator, false, s_clientEp, s_hostEp);

        Assert.AreNotEqual(baseline, baseline with { IsHost = true });
        Assert.AreNotEqual(baseline, baseline with { LocalEndpoint = s_hostEp });
        Assert.AreNotEqual(baseline, baseline with { HostEndpoint = null });
        Assert.AreNotEqual(baseline, baseline with { Communicator = new FakeCommunicator() });
    }

    /// <summary>
    /// 11. A with-expression copies the session and changes only the named value
    /// </summary>
    [TestMethod]
    public void WithExpression_ChangesOnlyNamedValue()
    {
        var original = new FileSyncSession(_communicator, false, s_clientEp, s_hostEp);

        FileSyncSession copy = original with { IsHost = true, HostEndpoint = null };

        Assert.IsTrue(copy.IsHost);
        Assert.IsNull(copy.HostEndpoint);
        Assert.AreSame(original.Communicator, copy.Communicator);
        Assert.AreEqual(original.LocalEndpoint, copy.LocalEndpoint);

        Assert.IsFalse(original.IsHost);
        Assert.AreEqual(s_hostEp, original.HostEndpoint);
    }

    /// <summary>
    /// 12. Deconstruction returns the values in constructor order
    /// </summary>
    [TestMethod]
    public void Deconstruct_ReturnsValuesInOrder()
    {
        var peers = new List<ControllerUser> { MakeUser("alice", 8002) };
        Action<IUserNotificationHandler> register = _ => { };
        Action<IUserNotificationHandler> unregister = _ => { };
        var session = new FileSyncSession(_communicator, false, s_clientEp, s_hostEp, peers, register, unregister);

        (ICommunicator communicator, bool isHost, IPEndPoint local, IPEndPoint? host,
            IEnumerable<ControllerUser>? initialPeers,
            Action<IUserNotificationHandler>? registerHandler,
            Action<IUserNotificationHandler>? unregisterHandler) = session;

        Assert.AreSame(_communicator, communicator);
        Assert.IsFalse(isHost);
        Assert.AreEqual(s_clientEp, local);
        Assert.AreEqual(s_hostEp, host);
        Assert.AreSame(peers, initialPeers);
        Assert.AreSame(register, registerHandler);
        Assert.AreSame(unregister, unregisterHandler);
    }
}
