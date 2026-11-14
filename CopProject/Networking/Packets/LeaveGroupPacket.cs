using System.Net;

namespace Networking.Packets;

public record LeaveGroupPacket(int MulticastGroup, IPEndPoint ClientIp) : NetworkMessage;
