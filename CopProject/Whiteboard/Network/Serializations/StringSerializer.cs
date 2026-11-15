/// Owners  : Raagam Hitesh Parmar 
/// Roll No : 112301024
/// LICENSE : MIT

using System.Text.Json;
using Whiteboard.Network.Dtos;
using Whiteboard.Network.Messages;

namespace Whiteboard.Network.Serializations;

/// <summary>
/// Serialize and deserialise an <see cref="ItemDto"/> using JSON
/// </summary>
public class ItemDtoStringSerializer
{
    /// <summary>
    /// Serialize from ItemDto to JSON
    /// </summary>
    /// <param name="item">Item to serialize</param>
    /// <returns>Serialized JSON</returns>
    public static string Serialize(ItemDto item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        return JsonSerializer.Serialize(item);
    }

    /// <summary>
    /// Deserialize from JSON to ItemDto
    /// </summary>
    /// <param name="json">Serialized JSON</param>
    /// <returns>Deserialized item</returns>
    public static ItemDto Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default!;
        }

        return JsonSerializer.Deserialize<ItemDto>(json)!;
    }
}

/// <summary>
/// Serialize and deserialise an <see cref="WhiteboardDto"/> using JSON
/// </summary>
public class WhiteboardDtoStringSerializer
{
    /// <summary>
    /// Serialize from WhiteboardDto to JSON
    /// </summary>
    /// <param name="item">Whiteboard to serialize</param>
    /// <returns>Serialized JSON</returns>
    public static string Serialize(WhiteboardDto item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        return JsonSerializer.Serialize(item);
    }

    /// <summary>
    /// Deserialize from JSON to WhiteboardDto
    /// </summary>
    /// <param name="json">Serialized JSON</param>
    /// <returns>Deserialized whiteboard DTO</returns>
    public static WhiteboardDto Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default!;
        }

        return JsonSerializer.Deserialize<WhiteboardDto>(json)!;
    }
}

/// <summary>
/// Serialize and deserialise an <see cref="ClientMessage"/> using JSON
/// </summary>
public class ClientMessageStringSerializer
{
    /// <summary>
    /// Serialize from ClientMessage to JSON
    /// </summary>
    /// <param name="item">ClientMessage to serialize</param>
    /// <returns>Serialized JSON</returns>
    public static string Serialize(ClientMessage item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        return JsonSerializer.Serialize(item);
    }

    /// <summary>
    /// Deserialize from JSON to ClientMessage
    /// </summary>
    /// <param name="json">Serialized JSON</param>
    /// <returns>Deserialized ClientMessage</returns>
    public static ClientMessage Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default!;
        }

        return JsonSerializer.Deserialize<ClientMessage>(json)!;
    }
}

/// <summary>
/// Serialize and deserialise an <see cref="ServerMessage"/> using JSON
/// </summary>
public class ServerMessageStringSerializer
{
    /// <summary>
    /// Serialize from ServerMessage to JSON
    /// </summary>
    /// <param name="item">ServerMessage to serialize</param>
    /// <returns>Serialized JSON</returns>
    public static string Serialize(ServerMessage item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        return JsonSerializer.Serialize(item);
    }

    /// <summary>
    /// Deserialize from JSON to ServerMessage
    /// </summary>
    /// <param name="json">Serialized JSON</param>
    /// <returns>Deserialized ServerMessage</returns>
    public static ServerMessage Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default!;
        }

        return JsonSerializer.Deserialize<ServerMessage>(json)!;
    }
}

