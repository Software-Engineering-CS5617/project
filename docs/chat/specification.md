# Specification

Specification for the chat module.

## Overview

Here is an overview of what things look like.

- All the people log into their systems.
- One person starts a meeting---they are the host.
- The other people provide an identifier to join the meeting---the host's IP
address for example.
- This creates a chatroom with all the participants, and one additional special
participant---our AI.
- The host's computer acts as the server.
- The attendees computers are clients.
- The server manages the chat.
- Chats do **not** persist across sessions.

## Features

### General features

- Messages.
    - Users can send messages in the chatroom, with textual and emoji content.
    - The UI shows the list of messages, along with the sender names and
    timestamps.
- Editing messages.
    - Users can edit their messages, and this explicitly shows an "edited" tag
    with the message.
    - Maybe we can have a time limit for editing messages.
- Deleting messages.
    - Users can delete their own messages (delete for everyone, no delete for
    me).
    - The UI shows something like "message deleted by <user>" in case a message
    is delete.
    - Maybe we can have a time limit for deleting messages as well.
- Replies.
    - Users can reply to messages.
    - The UI shows the reply as well as the message that is being replied to.
    - Users can go to the message being replied to by clicking on the same.
- Threads.
    - Users have an option to reply to a message in a thread.
    - If the thread does not currently exist, it is created with the reply as
    the first message in it.
    - If the thread exists, the message is added at the bottom of the thread.
    - Users can edit and delete messages in the thread as well, and the
    behavior here is very similar to the behavior in the main chatroom.
    - Users can reply to particular messages in a thread as well, but they
    cannot create another thread (there can only be one level of threading).
    - In case there is an important message that needs to be put in the thread
    as well as in the main chat, users can check a box to specify the same and
    the message shows up in the main chat as well. For such a message, the main
    chat's UI also provides a link to the thread.
- Reactions
    - Users can react to messages with a particular emoji.
    - Reaction counts are accumulated and displayed in the UI.
- Search
    - You can search for messages, and can specify various filters in the
    search query, such as:
        - Substring
        - Sender
        - A time range when the message was sent
    - This displays all the matching messages in a list, and clicking on them
    takes users to the respective messages

### AI features

- Chat with AI.
    - You can prompt the AI using `@AI`.
    - It can answer questions relevant to the chat.
    - The AI answers in the form of a message, replying to the prompt that it
    is answering.
- The AI can summarize the chat so far, maybe we have a button somewhere to do
that.
- You can use AI to get suggestions on improving the message being written.
    - The suggested message will be shown.
    - Users can accept or deny it.

## Stretch Goals

- Pinning messages.
    - Users can pin one message in the room.
    - This shows up as a line at the top of the chatroom, clicking on which
    takes users to the message.
    - Maybe we can have something where only some users can pin a message.
- Formatting and styling in messages.
    - This can be done markdown style.
    - `_italics_` or `*italics*` for _italics_, `**bold**` for **bold**, etc.
- Mentions.
    - You can mention a particular user in a message using `@<username>`.
    - There are special mentions as well, such as `@all` or `@everyone`.
    - If a message mentions you, that is specially highlighted for you.
- Better search.
    - Better string matching.
    - Sorting the output messages, the parameters can include:
        - Time
        - Time, reverse

