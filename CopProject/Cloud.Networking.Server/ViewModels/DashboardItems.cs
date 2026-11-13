// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking.Server.ViewModels;

/// <summary>
/// Refers to Connection in the ViewModel (as the format changes)
/// </summary>
/// <param name="ConnectionId">connection id</param>
/// <param name="ConnectedAt">connection creation time</param>
public sealed record ConnectionItem(
    string ConnectionId,
    string ConnectedAt
);

/// <summary>
/// Refers to a Message in the ViewModel
/// </summary>
/// <param name="Id">message id</param>
/// <param name="Time">message creation time</param>
/// <param name="From">sender id</param>
/// <param name="Content">message content</param>
/// <param name="IsFromServer">true if sender is the server</param>
public sealed record MessageItem(
    string Id,
    string Time,
    string From,
    string Content,
    bool IsFromServer
);
