/// Owners  : Raagam Hitesh Parmar 
/// Roll No : 112301024
/// LICENSE : MIT

using System.Text.Json;
using Microsoft.Windows.AI.ContentSafety;
using Networking.Packets;
using Whiteboard.Network.Dtos;
using Whiteboard.Network.Messages;

namespace Whiteboard.Network.Serializations;

/// <inheritdoc/>
public class ItemDtoByteSerializer : ISerializer<ItemDto>
{
    public byte[] Serialize(ItemDto item)
    {
        if (item is null)
        {
            return [];
        }

        return JsonSerializer.SerializeToUtf8Bytes(item);
    }

    public ItemDto Deserialize(byte[] data)
    {
        if (data is null || data.Length == 0)
        {
            return default!;
        }

        return JsonSerializer.Deserialize<ItemDto>(data)!;
    }
}

/// <inheritdoc/>
public class WhiteboardDtoByteSerializer : ISerializer<WhiteboardDto>
{
    public byte[] Serialize(WhiteboardDto item)
    {
        if (item is null)
        {
            return [];
        }

        return JsonSerializer.SerializeToUtf8Bytes(item);
    }

    public WhiteboardDto Deserialize(byte[] data)
    {
        if (data is null || data.Length == 0)
        {
            return default!;
        }

        return JsonSerializer.Deserialize<WhiteboardDto>(data)!;
    }
}

/// <inheritdoc/>
public class ClientMessageByteSerializer : ISerializer<ClientMessage>
{
    public byte[] Serialize(ClientMessage item)
    {
        if (item is null)
        {
            return [];
        }

        return JsonSerializer.SerializeToUtf8Bytes(item);
    }

    public ClientMessage Deserialize(byte[] data)
    {
        if (data is null || data.Length == 0)
        {
            return default!;
        }

        return JsonSerializer.Deserialize<ClientMessage>(data)!;
    }
}

/// <inheritdoc/>
public class ServerMessageByteSerializer : ISerializer<ServerMessage>
{
    public byte[] Serialize(ServerMessage item)
    {
        if (item is null)
        {
            return [];
        }

        return JsonSerializer.SerializeToUtf8Bytes(item);
    }

    public ServerMessage Deserialize(byte[] data)
    {
        if (data is null || data.Length == 0)
        {
            return default!;
        }

        return JsonSerializer.Deserialize<ServerMessage>(data)!;
    }
}
