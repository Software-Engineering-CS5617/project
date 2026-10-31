using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Networking.Packets;

public class PacketSerializer
{
    private Serializer<Packet> _packetSerializer = new Packets.Serializer<Packet>();
    public byte[] PacketToBytes(Packet packet)
    {
        byte[] serializedData = _packetSerializer.Serialize(packet);
        byte[] finalData = new byte[4 + serializedData.Length];
        BinaryPrimitives.WriteInt32BigEndian(finalData, serializedData.Length);
        Buffer.BlockCopy(serializedData, 0, finalData, 4, serializedData.Length);
        return finalData;
    }
    public Packet? BytesToPacket(byte[] data)
    {
        try
        {
            if (data == null || data.Length < 4)
            {
                Trace.WriteLine("Unable to process the data, data invalid");
                return null;
            }
            int payloadLength = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4));
            if (data.Length < 4 + payloadLength)
            {
                Trace.WriteLine("Payload length not matching the length in header");
            }
            byte[] payload = new byte[payloadLength];
            Buffer.BlockCopy(data, 4, payload, 0, payloadLength);
            return _packetSerializer.Deserialize(payload);
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[Networking] Error in BytesToPacket: {e.Message}");
            return null;
        }
        throw new NotImplementedException();
    }
}
