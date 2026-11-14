using System;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking.Packets;

namespace Networking.Test;

/// <summary>
/// Unit tests for <see cref="BinarySerialization"/>.
/// </summary>
[TestClass]
public class BinarySerializationTests
{
    private readonly BinarySerialization _serializer = new();

    [TestMethod]
    public void Serialize_NullPacket_ReturnsEmptyArray()
    {
        byte[] result = _serializer.Serialize(null!);
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void SerializeAndDeserialize_ValidIPv4Packet_PreservesAllFields()
    {
        byte[] payload = Encoding.UTF8.GetBytes("Binary serialization payload");
        IPEndPoint endpoint = new(IPAddress.Parse("192.168.1.100"), 8080);
        Packet original = new(payload, endpoint, 5, 2);

        byte[] serialized = _serializer.Serialize(original);
        Assert.IsNotNull(serialized);
        Assert.IsNotEmpty(serialized);

        Packet? deserialized = _serializer.Deserialize(serialized);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(5, deserialized.Module);
        Assert.AreEqual(2, deserialized.Priority);
        Assert.AreEqual(8080, deserialized.Destination.Port);
        Assert.AreEqual("192.168.1.100", deserialized.Destination.Address.ToString());
        CollectionAssert.AreEqual(payload, deserialized.SerializedData);
    }

    [TestMethod]
    public void SerializeAndDeserialize_ValidIPv6Packet_PreservesAllFields()
    {
        byte[] payload = Encoding.UTF8.GetBytes("IPv6 packet test");
        IPEndPoint endpoint = new(IPAddress.IPv6Loopback, 9050);
        Packet original = new(payload, endpoint, 1, 0);

        byte[] serialized = _serializer.Serialize(original);
        Assert.IsNotNull(serialized);

        Packet? deserialized = _serializer.Deserialize(serialized);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(1, deserialized.Module);
        Assert.AreEqual(0, deserialized.Priority);
        Assert.AreEqual(9050, deserialized.Destination.Port);
        Assert.AreEqual(IPAddress.IPv6Loopback, deserialized.Destination.Address);
        CollectionAssert.AreEqual(payload, deserialized.SerializedData);
    }

    [TestMethod]
    public void SerializeAndDeserialize_NullSerializedData_DeserializesToEmptyArray()
    {
        IPEndPoint endpoint = new(IPAddress.Loopback, 5000);
        Packet original = new(null!, endpoint, 2, 1);

        byte[] serialized = _serializer.Serialize(original);
        Assert.IsNotNull(serialized);

        Packet? deserialized = _serializer.Deserialize(serialized);
        Assert.IsNotNull(deserialized);
        Assert.IsNotNull(deserialized.SerializedData);
        Assert.IsEmpty(deserialized.SerializedData);
        Assert.AreEqual(endpoint.ToString(), deserialized.Destination.ToString());
    }

    [TestMethod]
    public void SerializeAndDeserialize_EmptySerializedData_PreservesEmptyArray()
    {
        IPEndPoint endpoint = new(IPAddress.Loopback, 5000);
        Packet original = new(Array.Empty<byte>(), endpoint, 4, 3);

        byte[] serialized = _serializer.Serialize(original);
        Assert.IsNotNull(serialized);

        Packet? deserialized = _serializer.Deserialize(serialized);
        Assert.IsNotNull(deserialized);
        Assert.IsNotNull(deserialized.SerializedData);
        Assert.IsEmpty(deserialized.SerializedData);
    }

    [TestMethod]
    public void SerializeAndDeserialize_NullDestination_DeserializesToDefaultEndpoint()
    {
        byte[] payload = Encoding.UTF8.GetBytes("No destination payload");
        Packet original = new(payload, null!, 3, 1);

        byte[] serialized = _serializer.Serialize(original);
        Assert.IsNotNull(serialized);

        Packet? deserialized = _serializer.Deserialize(serialized);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(IPAddress.Any, deserialized.Destination.Address);
        Assert.AreEqual(0, deserialized.Destination.Port);
        CollectionAssert.AreEqual(payload, deserialized.SerializedData);
    }

    [TestMethod]
    public void Deserialize_NullOrEmptyData_ReturnsNull()
    {
        Assert.IsNull(_serializer.Deserialize(null!));
        Assert.IsNull(_serializer.Deserialize(Array.Empty<byte>()));
    }

    [TestMethod]
    public void Deserialize_TruncatedHeader_ReturnsNull()
    {
        byte[] truncated = new byte[] { 0x01, 0x02 };
        Packet? result = _serializer.Deserialize(truncated);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Deserialize_IncompletePayload_ReturnsNull()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);
        writer.Write(100); // Claims payload length is 100 bytes
        writer.Write(new byte[10]); // Only writes 10 bytes
        writer.Flush();

        Packet? result = _serializer.Deserialize(stream.ToArray());
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Deserialize_NegativeDataLength_ReturnsEmptyData()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);
        writer.Write(-1); // dataLength < 0
        writer.Write(true); // hasDestination
        writer.Write("127.0.0.1");
        writer.Write(8080);
        writer.Write(1); // module
        writer.Write(2); // priority
        writer.Flush();

        Packet? result = _serializer.Deserialize(stream.ToArray());
        Assert.IsNotNull(result);
        Assert.IsEmpty(result.SerializedData);
        Assert.AreEqual(8080, result.Destination.Port);
        Assert.AreEqual(1, result.Module);
        Assert.AreEqual(2, result.Priority);
    }

    [TestMethod]
    public void Deserialize_InvalidIpString_ReturnsNull()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);
        writer.Write(0); // empty payload
        writer.Write(true); // hasDestination
        writer.Write("not_a_valid_ip_address");
        writer.Write(8080);
        writer.Write(1);
        writer.Write(1);
        writer.Flush();

        Packet? result = _serializer.Deserialize(stream.ToArray());
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Deserialize_InvalidPort_ThrowsExceptionAndReturnsNull()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);
        writer.Write(0); // empty payload
        writer.Write(true); // hasDestination
        writer.Write("127.0.0.1");
        writer.Write(999999); // invalid port > 65535, triggers ArgumentOutOfRangeException
        writer.Write(1);
        writer.Write(1);
        writer.Flush();

        Packet? result = _serializer.Deserialize(stream.ToArray());
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ISerializerInterface_Usage_Succeeds()
    {
        ISerializer<Packet> serializerInterface = new BinarySerialization();
        byte[] payload = Encoding.UTF8.GetBytes("Interface test");
        IPEndPoint endpoint = new(IPAddress.Loopback, 6000);
        Packet original = new(payload, endpoint, 1, 1);

        byte[] serialized = serializerInterface.Serialize(original);
        Packet? deserialized = serializerInterface.Deserialize(serialized);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(6000, deserialized.Destination.Port);
        CollectionAssert.AreEqual(payload, deserialized.SerializedData);
    }
}
