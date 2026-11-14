using System.Net;

namespace Networking.Packets;

public record JoinGroupPacket(int MulticastGroup, IPEndPoint ClientIp) : NetworkMessage;
