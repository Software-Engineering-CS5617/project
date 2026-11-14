using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace Networking.Packets;

/// <summary>
/// Provides binary serialization and deserialization for <see cref="Packet"/> instances
/// using <see cref="BinaryWriter"/> and <see cref="BinaryReader"/>.
/// </summary>
public class BinarySerialization : ISerializer<Packet>
{
    /// <summary>
    /// Serializes a <see cref="Packet"/> into a binary byte array using <see cref="BinaryWriter"/>.
    /// </summary>
    /// <param name="packet">The packet to serialize.</param>
    /// <returns>A byte array containing the serialized binary packet data, or empty array if packet is null.</returns>
    public byte[] Serialize(Packet packet)
    {
        if (packet == null)
        {
            Trace.TraceWarning("[Networking] BinarySerialization.Serialize: Packet is null.");
            return Array.Empty<byte>();
        }

        using var memoryStream = new MemoryStream();
        using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
        {
            if (packet.SerializedData == null)
            {
                writer.Write(-1);
            }
            else
            {
                writer.Write(packet.SerializedData.Length);
                writer.Write(packet.SerializedData);
            }

            if (packet.Destination == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                writer.Write(packet.Destination.Address.ToString());
                writer.Write(packet.Destination.Port);
            }

            writer.Write(packet.Module);
            writer.Write(packet.Priority);
            writer.Flush();
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Deserializes a binary byte array back into a <see cref="Packet"/> instance using <see cref="BinaryReader"/>.
    /// </summary>
    /// <param name="data">The byte array to deserialize.</param>
    /// <returns>The deserialized <see cref="Packet"/>, or null if deserialization fails.</returns>
    public Packet? Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            Trace.TraceWarning("[Networking] BinarySerialization.Deserialize: Data is null or empty.");
            return null;
        }

        try
        {
            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream, Encoding.UTF8, leaveOpen: false);

            int dataLength = reader.ReadInt32();
            byte[] serializedData;
            if (dataLength < 0)
            {
                serializedData = Array.Empty<byte>();
            }
            else
            {
                serializedData = reader.ReadBytes(dataLength);
                if (serializedData.Length != dataLength)
                {
                    Trace.TraceWarning("[Networking] BinarySerialization.Deserialize: Incomplete payload data.");
                    return null;
                }
            }

            bool hasDestination = reader.ReadBoolean();
            IPEndPoint destination = new IPEndPoint(IPAddress.Any, 0);
            if (hasDestination)
            {
                string ipString = reader.ReadString();
                int port = reader.ReadInt32();
                if (!IPAddress.TryParse(ipString, out IPAddress? ipAddress))
                {
                    Trace.TraceWarning($"[Networking] BinarySerialization.Deserialize: Invalid IP address '{ipString}'.");
                    return null;
                }

                destination = new IPEndPoint(ipAddress, port);
            }

            int module = reader.ReadInt32();
            int priority = reader.ReadInt32();

            return new Packet(serializedData, destination, module, priority);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[Networking] BinarySerialization.Deserialize error: {ex.Message}");
            return null;
        }
    }
}
