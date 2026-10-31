using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Networking.Packets;

public class IPEndPointConverter : JsonConverter<IPEndPoint>
{
    public override IPEndPoint? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? str = reader.GetString();
        return str != null ? IPEndPoint.Parse(str) : null;
    }

    public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class Serializer<T> : ISerializer<T>
{
    private readonly JsonSerializerOptions _options;

    public Serializer()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new IPEndPointConverter());
    }

    public byte[] Serialize(T item)
    {
        if (item == null)
        {
            return Array.Empty<byte>();
        }
        return JsonSerializer.SerializeToUtf8Bytes(item, _options);
    }

    public T? Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(data, _options);
    }
}
