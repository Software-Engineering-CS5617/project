using System.Net;

namespace Networking.Packets;

public record BroadcastPacket(byte[] Message, int Module, int Priority, IPEndPoint ClientIp) : NetworkMessage;
