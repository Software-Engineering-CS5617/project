using System.Net;

namespace Networking.Packets;

public record MulticastPacket(int MulticastGroup, byte[] Message, int Module, int Priority, IPEndPoint ClientIp) : NetworkMessage;
