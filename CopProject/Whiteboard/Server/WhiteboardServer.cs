// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Networking;
using Whiteboard.Interfaces;
using Whiteboard.Models;
using Whiteboard.Network.Dtos;
using Whiteboard.Network.Mappers;
using Whiteboard.Network.Messages;
using Whiteboard.Network.Serializations;
using Whiteboard.Services;

namespace Whiteboard.Server;

/// <inheritdoc/>
public class WhiteboardServer : IWhiteboardServer
{
    /// <summary>
    /// Server communicator
    /// </summary>
    private readonly ICommunicator _communicator;

    private readonly SharedWhiteboardHistoryService _history;
    // The history service relies on requests being processed one at a time.
    private readonly object _gate = new();

    public IWhiteboard Canvas { get; } = new WhiteboardCanvas();

    public WhiteboardServer(ICommunicator communicator)
    {
        ArgumentNullException.ThrowIfNull(communicator);

        _communicator = communicator;

        _history = new SharedWhiteboardHistoryService(Canvas);

        _communicator.AddSubscriber(Constants.WhiteboardModuleId, this);
    }

    /// <summary>
    /// Processes one request and broadcasts the canvas and history availability.
    /// </summary>
    /// <param name="message">Serialized client request.</param>
    public void OnMessageReceived(byte[] message)
    {
        ClientMessage request =
            new ClientMessageByteSerializer().Deserialize(message);

        // Requests need an actor identity before they can affect personal history.
        if (request is null ||
            string.IsNullOrWhiteSpace(request.UserId))
        {
            return;
        }

        // Keep requests and their broadcasts in the same order.
        lock (_gate)
        {
            _history.RegisterUser(request.UserId);

            switch (request)
            {
                case ClientMessage.ReqUpdate:
                    // No mutation: send the current state below.
                    break;

                // Reject mismatched IDs before passing the item to history.
                case ClientMessage.ReqAddItem(var id, var item)
                    when id == item.Metadata.Id:

                    _history.AddItem(
                        request.UserId,
                        ItemMapper.FromDto(item));
                    break;

                case ClientMessage.ReqDeleteItem(var id):
                    _history.DeleteItem(request.UserId, id);
                    break;

                case ClientMessage.ReqUpdateItem(var id, var item)
                    when id == item.Metadata.Id:

                    _history.EditItem(
                        request.UserId,
                        ItemMapper.FromDto(item));
                    break;

                case ClientMessage.ReqUndo:
                    _history.Undo(request.UserId);
                    break;

                case ClientMessage.ReqRedo:
                    _history.Redo(request.UserId);
                    break;

                case ClientMessage.ReqRestoreSnapshot(var whiteboard):
                    _history.RestoreState(request.UserId, whiteboard.Items.Select(ItemMapper.FromDto));
                    break;
            }

            BroadcastCanvas();
            BroadcastHistoryStates();
        }
    }


    /// <summary>
    /// Sends the complete canvas to all connected clients, including the requester.
    /// </summary>
    private void BroadcastCanvas()
    {
        var state = new WhiteboardDto(
            Canvas.Items.Values
                .Select(ItemMapper.ToDto)
                .ToList());

        Broadcast(new ServerMessage.ResUpdate(state));
    }

    /// <summary>
    /// Publishes each user's undo and redo availability for client-side filtering.
    /// </summary>
    private void BroadcastHistoryStates()
    {
#pragma warning disable IDE0008 // Use explicit type
        foreach (var state in _history.GetHistorySnapshots())
        {
            Broadcast(new ServerMessage.ResHistory(
                state.UserId,
                state.Undo,
                state.Redo));
        }
#pragma warning restore IDE0008 // Use explicit type
    }


    /// <summary>
    /// Broadcast a <see cref="ServerMessage"/> to all clients
    /// </summary>
    /// <param name="message">Server message to broadcast</param>
    private void Broadcast(ServerMessage message)
    {
        byte[] bytes = new ServerMessageByteSerializer().Serialize(message);
        _communicator.BroadcastMessage(bytes, Constants.WhiteboardModuleId, priority: Constants.WhiteboardModulePriority);
    }
}
