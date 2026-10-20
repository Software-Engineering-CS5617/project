# Architecture

Architecture for the chat module.

## Data structures and classes

> [!NOTE]
> In the subsections under this section, the class definitions only include
> attributes (and not methods). This is because these sections are meant to
> highlight the data structure aspect of the classes. The [methods
> section](#methods-and-interfaces) covers the functionality aspects.

### `Mentions`

The `Mentions` class is used to represent the users being mentioned in a
particular chat message -- the mention can either be `@all` or a set of some
usernames.

```cs
public sealed class Mentions
{
    // If true, every user is mentioned.
    public bool All { get; init; }

    // Specific usernames to mention, if All is false.
    public HashSet<string> Usernames { get; } = new();
}
```

### `ChatMessage`

A chat message is the fundamental building block of our module. It consists of
a textual message (with emojis, possibly), a timestamp to denote when it was
sent, the sender's name, etc.

The following class represents a basic chat message:

```cs
public class ChatMessage
{
    // When the message was sent.
    public DateTimeOffset Timestamp { get; init; }

    // Name of the sender.
    public required string SenderName { get; init; }

    // Mentions, if any.
    public Mentions Mentions { get; init; } = new();

    // Message content.
    public string Content { get; set; } = "";

    // Number of times the message's content has been edited.
    public int EditCount { get; set; }

    // The user who deleted the message (if any).
    public string? DeletedByUser { get; set; }

    // ID of the message that this one is a reply to (if any).
    public Guid? ParentMessageId;

    // Dictionary of emojis (their unicode value) and users who reacted with that emoji.
    public Dictionary<string, HashSet<string>> Reactions { get; } = new();
}
```

### `ChatMessageThreaded`

The previous class `ChatMessage` represents basic chat messages. Such messages
have no support for threading. So, we introduce a new class
`ChatMessageWithThreads` to represent chat messages from which threads can
arise.

Here is the `ChatMessageWithThreads` class:

```cs
public class ChatMessageWithThreads : ChatMessage
{
    // Two representations of messages in the associated thread.
    public readonly Dictionary<Guid, ChatMessage> ThreadById = new();
    public readonly SortedDictionary<(DateTimeOffset, Guid), ChatMessage> ThreadByTime = new();
}
```

> [!NOTE]
> The reason we have two structures for storing messages:
> - **Lookup by id:** `ThreadById` is used for $O(1)$ lookup of messages, keyed
>   by their message ID.
> - **Sorting by timestamp** `ThreadByTime` is used for maintaing an order of
>   messages with respect to the time they were sent.
>
> For more details, check out the [design decisions](#two-data-structures)
> section on this.

> [!NOTE]
> Observe that the `Thread` attribute is a list of `ChatMessage` values
> (**not** `ChatMessageWithThreads` values), which is how we implement the fact
> that threading can only be done at one level -- the main
> [chatroom](#chatroom) will have messages from which threads can arise, but
> the messages inside the thread cannot be used to generate another thread.

### `Chatroom`

A chatroom is where the conversations take place.

The following class represents our chatrooms:

```cs
public class Chatroom
{
    public string RoomName { get; set; }
    public string HostName { get; set; }
    public HashSet<string> Participants { get; } = new();

    private readonly Dictionary<Guid, ChatMessageWithThreads> _byId = new();
    private readonly SortedDictionary<(DateTimeOffset, Guid), ChatMessageWithThreads> _byTime = new();
}
```

> [!NOTE]
> The reason for two data structures for storing messages here is the same as
> that for `ChatMessageWithThreads`.
>
> For more details, check out the [design decisions](#two-data-structures)
> section on this.

Please note the following:

- Whenever a meeting is created, a chatroom for that meeting with is
initialized.
- Initially, the chatroom only has the host as a participant.
- As users join the meeting, they are added to this chatroom as participants.

## Methods and interfaces

This section contains the methods being provided to interact with the chat
module.

### `IMentions`

```cs
interface IMentions {
    bool IsUserMentioned(string username);
}
```


### `IChatMessage`

```cs
interface IChatMessage
{
    // Editing and deleting messages.
    public bool IsDeleted { get; }
    public void Edit(string newContent);
    public void Delete(string deleter);

    // Reactions.
    public bool AddReaction(string emoji, string user);
    public bool RemoveReaction(string emoji, string user);
}
```

### `IChatMessageWithThreads`

```cs
interface IChatMessageWithThreads : IChatMessage
{
    // Add message to the corresponding thread.
    public void AddMessageToThread(ChatMessage reply);
}
```

### `IChatroom`

Here is the interface for the `Chatroom` class.

```cs
enum RoomResult { Ok, NotFound, Forbidden }

interface IChatroom
{
    // Participants.
    bool AddParticipant(string name);
    bool RemoveParticipant(string name);

    // Messages.
    Guid AddMessage(ChatMessage message);
    bool TryGetMessage(Guid id, out ChatMessage message);

    // Editing and deleting messages.
    RoomResult EditMessage(Guid id, string newContent, string editor);
    RoomResult DeleteMessage(Guid id, string deleter);

    // Reactions.
    RoomResult AddReaction(Guid id, string emoji, string user);
    RoomResult RemoveReaction(Guid id, string emoji, string user);
}
```

Here is the meaning of values of the `RoomResult` enum:

| Value | Meaning |
|-|-|
| `Ok` | Everything was successful |
| `NotFound` | Invalid message ID -- such a message was not found |
| `Forbidden` | The requester did not have the appropriate permissions |

## Architecture

### Overview

Consider the context:

- A user starts (the _host_) a meeting and his device becomes the _server_.
- A chatroom is initialized on the server with the host being the sole
participant, initially.
- As users join the meeting, they join the chatroom and a copy of the chatroom
object is initialized on their devices as well.
- The initial chatroom's message content (in both of the data structures) is
empty.

### Control flow

The idea is to have a _version_ of a `Chatroom` object on all devices -- the
server as well as all of the clients. Any of these devices can perform some
operations, which will go through the server, and the server propagates these
updates to all the participants after updating its internal state. Each of the
clients, once they receive messages propagated by the server update their
internal state as well.

The sequence diagram below demonstrates the same:

```mermaid
sequenceDiagram
    autonumber
    participant S as Host (Server)
    participant C1 as Client 1
    participant C2 as Client 2

    Note over S: Host creates meeting,<br/>becomes the server.<br/>Chatroom created.

    C1->>S: JOIN_MEETING
    Note over S: Client 1 added to Participants

    C2->>S: JOIN_MEETING
    Note over S: Client 2 added to Participants

    C1->>S: NEW_MSG("Hi")
    Note over S: Assign id1, insert into<br/>MessagesById and MessagesByTime
    S->>C2: NEW_MSG(id1, "Hi")
    Note over C2: Local state updated:<br/>message id1 added

    C2->>S: REACT_TO_MSG(id1, 👍)
    Note over S: Add C2 to Reactions[👍] of id1
    S->>C1: REACT_TO_MSG(id1, 👍)
    Note over C1: Local state updated:<br/>reaction added to id1
```

### Operations

As the diagram in the section above showed, there are some "messages" that the
participants send to the server for every operation that they want to perform
with the chatroom, and the server further "propagates" these.

We have enumerated these operations. The following is a list of operations that
the clients can send to the server, along with the information encapsulated in
them. These have been divided into thread and non-thread operations.

- Client-specific operations:
    - Sync messages
    - Reset state
- General operations (for all clients):
    - Non-threaded versions (in the main chatroom):
        - Send new message: `ChatMessageWithThreads` object
        - Edit message: message ID, new content, the user attempting the edit
        - Delete message: message ID, the user attempting the deletion
        - Add reaction to message: message ID, reaction emoji, user attempting to add reaction
        - Remove reaction from message: message ID, reaction emoji, user attempting to remove reaction
    - Threaded versions (in a particular thread):
        - Send new message: parent message ID, `ChatMessageWithThreads` object
        - Edit message: parent message ID, message ID, new content, the user attempting the edit
        - Delete message: parent message ID, message ID, the user attempting the deletion
        - Add reaction to message: parent message ID, message ID, reaction emoji, user attempting to add reaction
        - Remove reaction from message: parent message ID, message ID, reaction emoji, user attempting to remove reaction

> [!NOTE]
> The threaded version of operations have exactly one additional argument
> compared to the non-threaded operations -- the parent message's ID.

The following classes capture the same:

```cs
public abstract record ChatRequest;

// Client-specific operations.

public sealed record Sync() : ChatRequest;

public sealed record Reset() : ChatRequest;

// General operations.

public sealed record SendMainMessage(ChatMessageWithThreads Message, Guid? ParentMessageId) : ChatRequest;

public sealed record EditMessage(Guid MessageId, string NewContent, string EditorName, Guid? ParentMessageId) : ChatRequest;

public sealed record DeleteMessage(Guid MessageId, string DeleterName, Guid? ParentMessageId) : ChatRequest;

public sealed record AddReaction(Guid MessageId, string Emoji, string ReactorName, Guid? ParentMessageId) : ChatRequest;

public sealed record RemoveReaction(Guid MessageId, string Emoji, string ReactorName, Guid? ParentMessageId) : ChatRequest;
```

> [!NOTE]
> Even though we have a total of 10 operations -- 5 threaded and 5
> non-threaded, it suffices to only have 5 records overall, because the last
> parameter of each of them (`Guid? ParentMessageId`) being `null` denotes the
> non-threaded variant, and the parameter being non-`null` denotes a threaded
> variant where the value of the parameter is the thread's parent message's ID.

### Implementation details

#### Server-side

Think of chatroom state as a non-negative integer. Assume that every client has
a `Chatroom` copy, and the server has a `Chatroom` copy as well. The server
additionally stores the state of every client's `Chatroom`.

An inefficient way to store the state of every client would be to store the
entire `Chatroom` data structure. Instead, we store a _non-negative integer_ as
representative of a `Chatroom` state -- the value 0 denotes the initialialized
empty state, and a value of `n`, for some `n > 0` denotes the first `n`
operations applied onto the initialized empty state.

So, the server stores a list of these operations, as well a map of
client-integer values to keep track of everything. In addition to this is its
own `Chatroom` object (as the server is also a participant in the chatroom).

Whenever the server receives a new operation from some client, it does the
following:

1. Applies that operation onto its own `Chatroom` object
2. Updates the operation list with this new operation
3. Updates the client states accordingly
    - For general operations (those that lead to updates for every client),
    iterates over the table of client states, and for each client:
        - Compares its state value with the latest state value (length of
        the operations list)
        - If there is a difference, send the appropriate sub-list of
        operations to the client
        - If successful, update the client's state to the latest one
    - For client-specific operations, attempts to send the appropriate
    operations to that client and update only it's state when successful

#### Client-side

The client maintains a `Chatroom` structure, and upon any operation, updates
its local copy, and sends the appropriate message to the server.

When it receives a message from the server, it updates the `Chatroom`
accordingly.

## Threads

Here are some rough ideas regarding threads:

- Broadcasting messages to multiple clients should be possible in parallel.
- Make sure to lock the server's state-related data structures when required.

## Design decisions

### Two data structures

All chat messages in a chatroom or thread can be found in two data structures:

1. `SortedDictionary`
2. `Dictionary`

`SortedDictionary` is used to sort the messages based on their timestamp. This
speeds up the following operations:

- Retreiving messages sorted by their timestamp
- Adding a new message while maintaining the order ($O(\log n)$)

`Dictionary` is used to store the messages, keyed by the message ID. This
speeds up retreival based on the message ID ($O(1)$, on average, in fact).

### Network-based broadcast vs server state

The decided approach records the state of all clients on the sever. An
alterative approach was considered, wherein the server does not maintain any
state other than it's own -- it is simply another client, and the networking
module's broadcast feature handles the broadcasting of messages from one client
to all other participants (which might include the server).

The current approach was preferred over network-based broadcast approach as the
latter does not have a single source-of-truth for the states. In case of any
state inconsistencies, it is challenging to reconcile the state. Such is not
the case for the approach that we go ahead with, where we explicitly provide
`SYNC` and `RESET` operations to synchronize state.

Also consider a scenario where a participant joins the meeting when 10 messages
have already been sent. With the network-based broadcast approach, they cannot
conveniently catch-up with the state of the chatroom, but with our approach,
they will be sent all current messages upon arrival.

