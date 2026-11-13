using System.Net;
using System.Text.Json.Serialization;

namespace Networking.Packets;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BroadcastPacket), typeDiscriminator: "broadcast")]
[JsonDerivedType(typeof(MulticastPacket), typeDiscriminator: "multicast")]
[JsonDerivedType(typeof(JoinGroupPacket), typeDiscriminator: "join")]
[JsonDerivedType(typeof(LeaveGroupPacket), typeDiscriminator: "leave")]
public abstract record NetworkMessage;
