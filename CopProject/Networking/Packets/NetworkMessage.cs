using System.Net;
using System.Text.Json.Serialization;

namespace Networking.Packets;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BroadcastPacket), typeDiscriminator: "broadcast")]
[JsonDerivedType(typeof(MulticastPacket), typeDiscriminator: "multicast")]
public abstract record NetworkMessage;
