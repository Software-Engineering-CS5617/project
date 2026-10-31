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
    }
}
