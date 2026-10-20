using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking.Packets
{
    public interface ISerializer<T>
    {
        byte[] Serialize(T item);
        T Deserialize(byte[] data);
    }
}
