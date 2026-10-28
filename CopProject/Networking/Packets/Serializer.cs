using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Networking.Packets
{
    public class Serializer<T> : ISerializer<T>
    {
        public XmlSerializer DataSerializer = new XmlSerializer(typeof(T));
        //public ISerializer DataSerializer = new JsonSerializer();

        public byte[] Serialize(T item)
        {
            if (item == null) return Array.Empty<byte>();
            using MemoryStream memoryStream = new MemoryStream();
            DataSerializer.Serialize(memoryStream, item);
            return memoryStream.ToArray();
        }
        public T Deserialize(byte[] data)
        {
            if (data == null || data.Length == 0) return default;
            using MemoryStream memoryStream = new MemoryStream(data);
            return (T)DataSerializer.Deserialize(memoryStream);
        }

    }
}
