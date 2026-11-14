using System.Text;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace Cloud.Crash;

/// <summary>
/// Serializes crash reports and crash groups to and from XML bytes.
/// </summary>
public static class CrashXmlSerializer
{
    private static readonly UTF8Encoding s_utf8 = new(false);

    /// <summary>
    /// Serializes a value to UTF-8 XML.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The XML bytes.</returns>
    public static byte[] Serialize<T>(T value)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        Trace.TraceInformation($"CrashXmlSerializer.Serialize: serializing {typeof(T).Name}.");

        XmlWriterSettings settings = new() {
            Encoding = s_utf8,
            Indent = true,
            // Keeps carriage returns in stack traces intact across a round trip.
            NewLineHandling = NewLineHandling.Entitize
        };

        XmlSerializerNamespaces namespaces = new();
        namespaces.Add(string.Empty, string.Empty);

        using MemoryStream stream = new();
        using XmlWriter writer = XmlWriter.Create(stream, settings);
        SerializerCache<T>.Instance.Serialize(writer, value, namespaces);
        writer.Flush();

        return stream.ToArray();
    }

    /// <summary>
    /// Deserializes a value from XML bytes. DTD processing is disabled because the data crosses a network.
    /// </summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="data">The XML bytes.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="InvalidDataException">The data is not valid XML for the requested type.</exception>
    public static T Deserialize<T>(byte[] data)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(data);

        XmlReaderSettings settings = new() {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        try
        {
            using MemoryStream stream = new(data);
            using XmlReader reader = XmlReader.Create(stream, settings);
            object? result = SerializerCache<T>.Instance.Deserialize(reader);

            if (result is not T typed)
            {
                throw new InvalidDataException("The crash XML was empty.");
            }

            Trace.TraceInformation($"CrashXmlSerializer.Deserialize: deserialized {typeof(T).Name}.");
            return typed;
        }
        catch (Exception ex) when (ex is InvalidOperationException or XmlException)
        {
            Trace.TraceWarning($"CrashXmlSerializer.Deserialize: invalid {typeof(T).Name} XML: {ex.Message}");
            throw new InvalidDataException("The crash XML is not valid.", ex);
        }
    }

    private static class SerializerCache<T>
    {
        public static readonly XmlSerializer Instance = new(typeof(T));
    }
}
