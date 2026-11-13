using System.Text.Json.Serialization;

namespace Chat;

/// <summary>
/// Base type for actions and synchronization requests sent by a client.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$request")]
[JsonDerivedType(typeof(Sync), "sync")] [JsonDerivedType(typeof(Reset), "reset")] [JsonDerivedType(typeof(SendMessage), "send")]
[JsonDerivedType(typeof(EditMessage), "edit")]
[JsonDerivedType(typeof(DeleteMessage), "delete")]
[JsonDerivedType(typeof(ToggleReaction), "toggleReaction")]
public abstract record ChatClientRequest;

/// <summary>
/// Requests synchronization with the server's current operation log.
/// </summary>
public sealed record Sync() : ChatClientRequest;

/// <summary>
/// Requests a reset of local state followed by a complete synchronization.
/// </summary>
public sealed record Reset() : ChatClientRequest;

/// <summary>
/// Base type for requests that change the chatroom.
/// </summary>
public abstract record ChatRoomChangeRequest : ChatClientRequest;

/// <summary>
/// Sends a message to the room or to a message's thread.
/// </summary>
/// <param name="Timestamp">The time at which the message was composed.</param>
/// <param name="Mentions">The users mentioned by the message.</param>
/// <param name="Content">The message content.</param>
/// <param name="ThreadParentMessageId">The thread root message ID, or <see langword="null"/> for the room.</param>
/// <param name="ReplyToMessageId">The room message this message replies to inline.</param>
public sealed record SendMessage(
    DateTimeOffset Timestamp,
    Mentions Mentions,
    string Content,
    Guid? ThreadParentMessageId,
    Guid? ReplyToMessageId = null) : ChatRoomChangeRequest;

/// <summary>
/// Requests an edit to a room message or thread message.
/// </summary>
/// <param name="MessageId">The message ID.</param>
/// <param name="NewContent">The replacement content.</param>
/// <param name="ThreadParentMessageId">The thread root message ID, or <see langword="null"/> for the room.</param>
public sealed record EditMessage(
    Guid MessageId,
    string NewContent,
    Guid? ThreadParentMessageId) : ChatRoomChangeRequest;

/// <summary>
/// Requests deletion of a room message or thread message.
/// </summary>
/// <param name="MessageId">The message ID.</param>
/// <param name="ThreadParentMessageId">The thread root message ID, or <see langword="null"/> for the room.</param>
public sealed record DeleteMessage(Guid MessageId, Guid? ThreadParentMessageId) : ChatRoomChangeRequest;

/// <summary>
/// Toggles a reaction on a room message or thread message.
/// </summary>
/// <param name="MessageId">The message ID.</param>
/// <param name="Emoji">The reaction emoji.</param>
/// <param name="ThreadParentMessageId">The thread root message ID, or <see langword="null"/> for the room.</param>
public sealed record ToggleReaction(
    Guid MessageId,
    string Emoji,
    Guid? ThreadParentMessageId) : ChatRoomChangeRequest;

/// <summary>
/// Wraps a client request with its ID and username.
/// </summary>
/// <param name="RequestId">The client-generated request ID.</param>
/// <param name="Request">The request body.</param>
/// <param name="Username">The client username asserted with the request.</param>
public sealed record ChatClientRequestEnvelope(
    Guid RequestId,
    ChatClientRequest Request,
    string Username);

/// <summary>
/// Base type for accepted room changes recorded for replay.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$operation")]
[JsonDerivedType(typeof(MessageAdded), "messageAdded")]
[JsonDerivedType(typeof(MessageEdited), "messageEdited")]
[JsonDerivedType(typeof(MessageDeleted), "messageDeleted")]
[JsonDerivedType(typeof(ReactionToggled), "reactionToggled")]
public abstract record ChatOperation;

/// <summary>
/// Records a message added to the room or a thread.
/// </summary>
/// <param name="MessageId">The ID assigned to the message.</param>
/// <param name="Message">The added message.</param>
/// <param name="ThreadParentMessageId">The thread root message ID, or <see langword="null"/> for a room message.</param>
public sealed record MessageAdded(
    Guid MessageId,
    ChatMessageWithThreads Message,
    Guid? ThreadParentMessageId = null) : ChatOperation;

/// <summary>
/// Records a message edit.
/// </summary>
/// <param name="MessageId">The edited message ID.</param>
/// <param name="NewContent">The replacement content.</param>
/// <param name="ActorUsername">The username that performed the edit.</param>
/// <param name="ThreadParentMessageId">The thread root message ID, or <see langword="null"/> for a room message.</param>
public sealed record MessageEdited(
    Guid MessageId,
    string NewContent,
    string ActorUsername,
    Guid? ThreadParentMessageId) : ChatOperation;

/// <summary>
/// Records a message deletion.
/// </summary>
/// <param name="MessageId">The deleted message ID.</param>
/// <param name="ActorUsername">The username that deleted the message.</param>
/// <param name="ThreadParentMessageId">The thread root message ID, or <see langword="null"/> for a room message.</param>
public sealed record MessageDeleted(
    Guid MessageId,
    string ActorUsername,
    Guid? ThreadParentMessageId) : ChatOperation;

/// <summary>
/// Records a reaction toggle on a message.
/// </summary>
/// <param name="MessageId">The message ID.</param>
/// <param name="Emoji">The reaction emoji.</param>
/// <param name="ActorUsername">The username that toggled the reaction.</param>
/// <param name="ThreadParentMessageId">The thread root message ID, or <see langword="null"/> for a room message.</param>
public sealed record ReactionToggled(
    Guid MessageId,
    string Emoji,
    string ActorUsername,
    Guid? ThreadParentMessageId) : ChatOperation;

/// <summary>
/// Applies accepted chat operations to a room.
/// </summary>
public static class ChatProtocol
{
    /// <summary>
    /// Applies a server-approved operation to the specified room.
    /// </summary>
    /// <param name="chatroom">The room to update.</param>
    /// <param name="operation">The operation to apply.</param>
    /// <returns>The result of applying the operation.</returns>
    public static RoomResult ApplyOperation(Chatroom chatroom, ChatOperation operation)
    {
        ArgumentNullException.ThrowIfNull(chatroom);
        ArgumentNullException.ThrowIfNull(operation);

        switch (operation)
        {
            case MessageAdded added:
                ChatMessageWithThreads message = added.Message.DeepCloneWithoutThreadMessages();
                if (added.ThreadParentMessageId is Guid parentId)
                {
                    return chatroom.AddThreadMessageById(parentId, added.MessageId, message);
                }

                chatroom.AddMessage(added.MessageId, message);
                return RoomResult.Ok;

            case MessageEdited edit:
                return chatroom.EditMessage(
                    edit.MessageId,
                    edit.NewContent,
                    edit.ActorUsername,
                    edit.ThreadParentMessageId);

            case MessageDeleted delete:
                return chatroom.DeleteMessage(
                    delete.MessageId,
                    delete.ActorUsername,
                    delete.ThreadParentMessageId);

            case ReactionToggled reaction:
                return chatroom.ToggleReaction(
                    reaction.MessageId,
                    reaction.Emoji,
                    reaction.ActorUsername,
                    reaction.ThreadParentMessageId);

            default:
                return RoomResult.Forbidden;
        }
    }
}

/// <summary>
/// Base type for server updates that clients apply to their local room.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$update")]
[JsonDerivedType(typeof(ChatServerReset), "reset")]
[JsonDerivedType(typeof(ChatServerOperationBatch), "operations")]
public abstract record ChatServerUpdate;

/// <summary>
/// Tells a client to clear its messages before replaying the operation history.
/// </summary>
public sealed record ChatServerReset() : ChatServerUpdate;

/// <summary>
/// Carries an ordered range of room changes for a client to apply.
/// </summary>
/// <param name="FromVersion">The client's expected starting operation count.</param>
/// <param name="ToVersion">The operation count after applying the batch.</param>
/// <param name="Operations">The ordered room changes.</param>
public sealed record ChatServerOperationBatch(
    int FromVersion,
    int ToVersion,
    IReadOnlyList<ChatOperation> Operations) : ChatServerUpdate;

/// <summary>
/// Wraps a server room update with the current participant snapshot.
/// </summary>
/// <param name="Update">The room update.</param>
/// <param name="Participants">The current room participants.</param>
public sealed record ChatServerUpdateEnvelope(
    ChatServerUpdate Update,
    IReadOnlyList<string> Participants) : ChatServerMessage;

/// <summary>
/// Base type for server messages sent to a client.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$response")]
[JsonDerivedType(typeof(ChatServerRequestResult), "result")]
[JsonDerivedType(typeof(ChatServerUpdateEnvelope), "update")]
public abstract record ChatServerMessage;

/// <summary>
/// Reports the result of an individual client request.
/// </summary>
/// <param name="RequestId">The corresponding client request ID.</param>
/// <param name="Result">The operation result.</param>
public sealed record ChatServerRequestResult(
    Guid RequestId,
    RoomResult Result) : ChatServerMessage;
