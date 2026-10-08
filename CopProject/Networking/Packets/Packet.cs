using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Networking.Packets
{
    public record Packet(byte[] SerializedData, IPEndPoint Destination, int Module, int Priority);
}
