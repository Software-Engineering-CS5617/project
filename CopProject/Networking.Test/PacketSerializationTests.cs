using System;
using System.Buffers.Binary;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking.Packets;

namespace Networking.Test;

/// <summary>
/// Unit tests for PacketSerializer and Serializer.
/// </summary>
[TestClass]
public class PacketSerializationTests
{
    /// <summary>
    /// Checks that packet serialization and deserialization preserves all fields.
    /// </summary>
    [TestMethod]
    public void Serializer_SerializeAndDeserialize_PreservesPacketData()
    {
        Serializer<Packet> serializer = new();
        byte[] payload = Encoding.UTF8.GetBytes("Test Message Payload");
        IPEndPoint endpoint = new(IPAddress.Parse("127.0.0.1"), 8080);
        Packet original = new(payload, endpoint, 3, 1);

        byte[] serialized = serializer.Serialize(original);
        Packet? deserialized = serializer.Deserialize(serialized);

        Assert.IsNotNull(serialized);
        Assert.IsGreaterThan(0, serialized.Length);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(3, deserialized.Module);
        Assert.AreEqual(1, deserialized.Priority);
        Assert.AreEqual(8080, deserialized.Destination.Port);
        Assert.AreEqual("127.0.0.1", deserialized.Destination.Address.ToString());
        CollectionAssert.AreEqual(payload, deserialized.SerializedData);
    }

    /// <summary>
    /// Checks that Serializer handles null or empty inputs safely.
    /// </summary>
    [TestMethod]
    public void Serializer_NullOrEmptyInputs_ReturnsSafeDefaults()
    {
        Serializer<Packet> serializer = new();

        byte[] nullResult = serializer.Serialize(null!);
        Packet? emptyResult = serializer.Deserialize(Array.Empty<byte>());
        Packet? nullDataResult = serializer.Deserialize(null!);

        CollectionAssert.AreEqual(Array.Empty<byte>(), nullResult);
        Assert.IsNull(emptyResult);
        Assert.IsNull(nullDataResult);
    }

    /// <summary>
    /// Checks length-prefixed framing in PacketSerializer.
    /// </summary>
    [TestMethod]
    public void PacketSerializer_PacketToBytesAndBytesToPacket_RoundTripsSuccessfully()
    {
        PacketSerializer packetSerializer = new();
        byte[] payload = Encoding.UTF8.GetBytes("Framed Packet Data");
        IPEndPoint endpoint = new(IPAddress.Loopback, 9090);
        Packet packet = new(payload, endpoint, 42, 2);

        byte[] framedBytes = packetSerializer.PacketToBytes(packet);

        Assert.IsNotNull(framedBytes);
        Assert.IsGreaterThan(4, framedBytes.Length);

        int payloadLength = BinaryPrimitives.ReadInt32BigEndian(framedBytes.AsSpan(0, 4));
        Assert.AreEqual(framedBytes.Length - 4, payloadLength);

        Packet? result = packetSerializer.BytesToPacket(framedBytes);

        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.Module);
        Assert.AreEqual(2, result.Priority);
        Assert.AreEqual(9090, result.Destination.Port);
        CollectionAssert.AreEqual(payload, result.SerializedData);
    }

    /// <summary>
    /// Checks that data shorter than the 4-byte header returns null.
    /// </summary>
    [TestMethod]
    public void PacketSerializer_BytesToPacket_IncompleteHeader_ReturnsNull()
    {
        PacketSerializer packetSerializer = new();

        Packet? nullResult = packetSerializer.BytesToPacket(null!);
        Packet? shortResult = packetSerializer.BytesToPacket(new byte[] { 0x00, 0x01 });

        Assert.IsNull(nullResult);
        Assert.IsNull(shortResult);
    }

    /// <summary>
    /// Checks that a payload smaller than declared in the header returns null.
    /// </summary>
    [TestMethod]
    public void PacketSerializer_BytesToPacket_MismatchedPayloadLength_ReturnsNull()
    {
        PacketSerializer packetSerializer = new();
        byte[] malformed = new byte[6];
        BinaryPrimitives.WriteInt32BigEndian(malformed, 100);

        Packet? result = packetSerializer.BytesToPacket(malformed);

        Assert.IsNull(result);
    }

    /// <summary>
    /// Checks JSON serialization of IPEndPoint using the custom converter.
    /// </summary>
    [TestMethod]
    public void IPEndPointConverter_DirectJsonSerialization_RoundTripsEndpoint()
    {
        JsonSerializerOptions options = new();
        options.Converters.Add(new IPEndPointConverter());
        IPEndPoint endpoint = new(IPAddress.Parse("192.168.1.100"), 5050);

        string json = JsonSerializer.Serialize(endpoint, options);
        IPEndPoint? deserialized = JsonSerializer.Deserialize<IPEndPoint>(json, options);

        Assert.IsNotNull(json);
        Assert.AreEqual("\"192.168.1.100:5050\"", json);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(5050, deserialized.Port);
        Assert.AreEqual("192.168.1.100", deserialized.Address.ToString());

        IPEndPoint? nullDeserialized = JsonSerializer.Deserialize<IPEndPoint>("null", options);
        Assert.IsNull(nullDeserialized);
    }

    /// <summary>
    /// Checks that BroadcastPacket serialization and deserialization preserves all fields.
    /// </summary>
    [TestMethod]
    public void Serializer_BroadcastPacket_RoundTripsSuccessfully()
    {
        Serializer<BroadcastPacket> serializer = new();
        byte[] payload = Encoding.UTF8.GetBytes("Broadcast Payload");
        IPEndPoint endpoint = new(IPAddress.Loopback, 5000);
        BroadcastPacket packet = new(payload, 10, 2, endpoint);

        byte[] serialized = serializer.Serialize(packet);
        BroadcastPacket? deserialized = serializer.Deserialize(serialized);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(10, deserialized.Module);
        Assert.AreEqual(2, deserialized.Priority);
        Assert.AreEqual(5000, deserialized.ClientIp.Port);
        CollectionAssert.AreEqual(payload, deserialized.Message);
    }

    /// <summary>
    /// Checks that MulticastPacket serialization and deserialization preserves all fields.
    /// </summary>
    [TestMethod]
    public void Serializer_MulticastPacket_RoundTripsSuccessfully()
    {
        Serializer<MulticastPacket> serializer = new();
        byte[] payload = Encoding.UTF8.GetBytes("Multicast Payload");
        IPEndPoint endpoint = new(IPAddress.Loopback, 6000);
        MulticastPacket packet = new(77, payload, 12, 1, endpoint);

        byte[] serialized = serializer.Serialize(packet);
        MulticastPacket? deserialized = serializer.Deserialize(serialized);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(77, deserialized.MulticastGroup);
        Assert.AreEqual(12, deserialized.Module);
        Assert.AreEqual(1, deserialized.Priority);
        Assert.AreEqual(6000, deserialized.ClientIp.Port);
        CollectionAssert.AreEqual(payload, deserialized.Message);
    }

    /// <summary>
    /// Checks polymorphic NetworkMessage serialization and deserialization for BroadcastPacket and MulticastPacket.
    /// </summary>
    [TestMethod]
    public void Serializer_NetworkMessage_PolymorphicRoundTripsSuccessfully()
    {
        Serializer<NetworkMessage> serializer = new();

        BroadcastPacket broadcast = new(Encoding.UTF8.GetBytes("Polymorphic Broadcast"), 10, 1, new IPEndPoint(IPAddress.Loopback, 5001));
        byte[] broadcastBytes = serializer.Serialize(broadcast);
        NetworkMessage? deserializedBroadcast = serializer.Deserialize(broadcastBytes);

        Assert.IsNotNull(deserializedBroadcast);
        Assert.IsInstanceOfType<BroadcastPacket>(deserializedBroadcast);
        BroadcastPacket castBroadcast = (BroadcastPacket)deserializedBroadcast;
        Assert.AreEqual(10, castBroadcast.Module);
        CollectionAssert.AreEqual(broadcast.Message, castBroadcast.Message);

        MulticastPacket multicast = new(88, Encoding.UTF8.GetBytes("Polymorphic Multicast"), 11, 2, new IPEndPoint(IPAddress.Loopback, 5002));
        byte[] multicastBytes = serializer.Serialize(multicast);
        NetworkMessage? deserializedMulticast = serializer.Deserialize(multicastBytes);

        Assert.IsNotNull(deserializedMulticast);
        Assert.IsInstanceOfType<MulticastPacket>(deserializedMulticast);
        MulticastPacket castMulticast = (MulticastPacket)deserializedMulticast;
        Assert.AreEqual(88, castMulticast.MulticastGroup);
        CollectionAssert.AreEqual(multicast.Message, castMulticast.Message);

        JoinGroupPacket join = new(42, new IPEndPoint(IPAddress.Loopback, 5003));
        byte[] joinBytes = serializer.Serialize(join);
        NetworkMessage? deserializedJoin = serializer.Deserialize(joinBytes);

        Assert.IsNotNull(deserializedJoin);
        Assert.IsInstanceOfType<JoinGroupPacket>(deserializedJoin);
        JoinGroupPacket castJoin = (JoinGroupPacket)deserializedJoin;
        Assert.AreEqual(42, castJoin.MulticastGroup);
        Assert.AreEqual(5003, castJoin.ClientIp.Port);

        LeaveGroupPacket leave = new(42, new IPEndPoint(IPAddress.Loopback, 5003));
        byte[] leaveBytes = serializer.Serialize(leave);
        NetworkMessage? deserializedLeave = serializer.Deserialize(leaveBytes);

        Assert.IsNotNull(deserializedLeave);
        Assert.IsInstanceOfType<LeaveGroupPacket>(deserializedLeave);
        LeaveGroupPacket castLeave = (LeaveGroupPacket)deserializedLeave;
        Assert.AreEqual(42, castLeave.MulticastGroup);
        Assert.AreEqual(5003, castLeave.ClientIp.Port);
    }

    /// <summary>
    /// Checks that malformed payload data returns null via catch block.
    /// </summary>
    [TestMethod]
    public void PacketSerializer_CorruptedPayload_ReturnsNull()
    {
        PacketSerializer serializer = new();
        byte[] corrupted = new byte[] { 0, 0, 0, 4, 0xFF, 0xFF, 0xFF, 0xFF };

        Packet? result = serializer.BytesToPacket(corrupted);
        Assert.IsNull(result);
    }

    /// <summary>
    /// Checks IPEndPointConverter direct read with null token.
    /// </summary>
    [TestMethod]
    public void IPEndPointConverter_DirectRead_HandlesNull()
    {
        IPEndPointConverter converter = new();
        byte[] json = Encoding.UTF8.GetBytes("null");
        Utf8JsonReader reader = new(json);
        reader.Read();
        IPEndPoint? ep = converter.Read(ref reader, typeof(IPEndPoint), new JsonSerializerOptions());
        Assert.IsNull(ep);
    }
}
