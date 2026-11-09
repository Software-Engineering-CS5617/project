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

    /// <inheritdoc/>
    public bool CanUndo { get; private set; }

    /// <inheritdoc/>
    public bool CanRedo { get; private set; }

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

    public void RequestSync()
    {
        SendToServer(new ClientMessage.ReqUpdate());
    }

    public void AddItem(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        //Canvas.AddItem(item);
        SendToServer(new ClientMessage.ReqAddItem(item.Id, ItemMapper.ToDto(item)));
    }

    public void DeleteItem(Guid id)
    {   
        //Canvas.DeleteItem(id);
        SendToServer(new ClientMessage.ReqDeleteItem(id));
    }

    public void EditItem(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        //Canvas.EditItem(item);
        SendToServer(new ClientMessage.ReqUpdateItem(item.Id, ItemMapper.ToDto(item)));
    }

    /// <inheritdoc/>
    public void Undo()
    {
        SendToServer(new ClientMessage.ReqUndo());
    }

    /// <inheritdoc/>
    public void Redo()
    {
        SendToServer(new ClientMessage.ReqRedo());
    }

    /// <summary>
    /// Applies a full canvas response or updates this user's history availability.
    /// </summary>
    /// <param name="message">Serialized server message.</param>
    public void OnMessageReceived(byte[] message)
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

                    break;
                }

            // History states are broadcast; only apply the current user's state.
            case ServerMessage.ResHistory(
                var userId,
                var canUndo,
                var canRedo)
                when string.Equals(
                    userId,
                    UserId,
                    StringComparison.Ordinal):

                CanUndo = canUndo;
                CanRedo = canRedo;
                HistoryChanged();
                break;
        }
    }
}
