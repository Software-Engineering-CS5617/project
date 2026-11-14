// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Net;
using Networking;
using Whiteboard.Interfaces;
using Whiteboard.Models;
using Whiteboard.Network.Dtos;
using Whiteboard.Network.Mappers;
using Whiteboard.Network.Messages;
using Whiteboard.Network.Serializations;
using System.Collections.Generic;
using System.Diagnostics;

namespace Whiteboard.Client;

/// <inheritdoc/>
public class WhiteboardClient : IWhiteboardClient
{
    /// <summary>
    /// Client communicator
    /// </summary>
    private readonly ICommunicator _communicator;

    /// <summary>
    /// Endpoint of the server
    /// </summary>
    private readonly IPEndPoint _serverEndpoint;

    public IWhiteboard Canvas { get; } = new WhiteboardCanvas();

    public string UserId { get; }

    private List<HistoryEntryDto> _undo = new();
    private List<HistoryEntryDto> _redo = new();

    /// <inheritdoc/>
    public bool CanUndo => _undo.Count > 0;

    /// <inheritdoc/>
    public bool CanRedo => _redo.Count > 0;


    /// <inheritdoc/>
    public event Action HistoryChanged = delegate { };

    public WhiteboardClient(ICommunicator communicator, IPEndPoint serverEndpoint, string userId)
    {
        ArgumentNullException.ThrowIfNull(communicator);
        ArgumentNullException.ThrowIfNull(serverEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        _communicator = communicator;
        _serverEndpoint = serverEndpoint;
        UserId = userId;

        _communicator.AddSubscriber(Constants.WhiteboardModuleId, this);
        RequestSync();
    }

    /// <summary>
    /// Send <see cref="ClientMessage"/> to the server
    /// </summary>
    /// <param name="msg">Client message to send</param>
    private void SendToServer(ClientMessage message)
    {
        try
        {
            // History follows the actor, which can differ from an item's creator.
            ClientMessage request = message with { UserId = UserId };

            byte[] bytes =
                new ClientMessageByteSerializer().Serialize(request);

            _communicator.SendMessage(
                _serverEndpoint,
                bytes,
                Constants.WhiteboardModuleId,
                priority: Constants.WhiteboardModulePriority);
        }
        catch (Exception exception)
        {
            Trace.TraceError($"[WhiteboardClient][Error] Request serialization or send failed. User={UserId}. {exception}");
            throw;
        }
    }

    public void RequestSync()
    {
        Trace.TraceInformation($"[WhiteboardClient] Canvas synchronization requested. User={UserId}");
        SendToServer(new ClientMessage.ReqUpdate());
    }

    public void AddItem(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        Canvas.AddItem(item);
        Trace.TraceInformation($"[WhiteboardClient] Add requested. User={UserId}, Item={item.Id}");
        SendToServer(new ClientMessage.ReqAddItem(item.Id, ItemMapper.ToDto(item)));
    }

    public void DeleteItem(Guid id)
    {
        Canvas.DeleteItem(id);
        Trace.TraceInformation($"[WhiteboardClient] Delete requested. User={UserId}, Item={id}");
        SendToServer(new ClientMessage.ReqDeleteItem(id));
    }

    public void EditItem(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        Canvas.EditItem(item);
        Trace.TraceInformation($"[WhiteboardClient] Edit requested. User={UserId}, Item={item.Id}");
        SendToServer(new ClientMessage.ReqUpdateItem(item.Id, ItemMapper.ToDto(item)));
    }

    /// <inheritdoc/>
    public void Undo()
    {
        Trace.TraceInformation($"[WhiteboardClient] Undo requested. User={UserId}, Undo={_undo.Count}, Redo={_redo.Count}");
        SendToServer(new ClientMessage.ReqUndo());
    }

    /// <inheritdoc/>
    public void Redo()
    {
        Trace.TraceInformation($"[WhiteboardClient] Redo requested. User={UserId}, Undo={_undo.Count}, Redo={_redo.Count}");
        SendToServer(new ClientMessage.ReqRedo());
    }

    /// <summary>
    /// Applies a full canvas response or updates this user's history availability.
    /// </summary>
    /// <param name="message">Serialized server message.</param>
    public void OnMessageReceived(byte[] message)
    {
        try
        {
            ServerMessage response =
                new ServerMessageByteSerializer().Deserialize(message);

            switch (response)
            {
                case ServerMessage.ResUpdate(var state):
                    {
                        // Convert first, before removing the current state.
                        IItem[] items = state.Items
                            .Select(ItemMapper.FromDto)
                            .ToArray();

                        // Delete through the model so the renderer removes stale visuals.
                        // Clearing the dictionary directly would not raise deletion events.
                        foreach (Guid id in Canvas.Items.Keys.ToArray())
                        {
                            Canvas.DeleteItem(id);
                        }

                        foreach (IItem item in items)
                        {
                            Canvas.AddItem(item);
                        }

                        Trace.TraceInformation($"[WhiteboardClient] Server canvas applied. User={UserId}, Items={items.Length}");
                        break;
                    }

                // History states are broadcast; only apply the current user's state.
                case ServerMessage.ResHistory(
                    var userId,
                    var undo,
                    var redo)
                    when string.Equals(
                        userId,
                        UserId,
                        StringComparison.Ordinal):

                    _undo = undo;
                    _redo = redo;
                    Trace.TraceInformation($"[WhiteboardClient] Personal history updated. User={UserId}, Undo={_undo.Count}, Redo={_redo.Count}");
                    HistoryChanged();
                    break;

                // Other users' history broadcasts are expected and need no warning.
                case ServerMessage.ResHistory:
                    break;

                default:
                    Trace.TraceWarning($"[WhiteboardClient][Warning] Empty or unsupported server response ignored. User={UserId}");
                    break;
            }
        }
        catch (Exception exception)
        {
            Trace.TraceError($"[WhiteboardClient][Error] Server response processing failed. User={UserId}. {exception}");
            throw;
        }
    }

    public void RestoreSnapshot(WhiteboardDto whiteboard)
    {
        ArgumentNullException.ThrowIfNull(whiteboard);
        Trace.TraceInformation($"[WhiteboardClient] Snapshot restoration requested. User={UserId}, Items={whiteboard.Items.Count}");
        SendToServer(new ClientMessage.ReqRestoreSnapshot(whiteboard));
    }
}
