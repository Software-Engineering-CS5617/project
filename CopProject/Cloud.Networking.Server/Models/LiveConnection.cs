// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking.Server.Models;

/// <summary>
/// A persistent connection to a server
/// </summary>
/// <param name="ConnectionId">the id of a <see cref="LiveConnection"/></param>
/// <param name="ConnectedAt">the creation timestamp of the <see cref="LiveConnection"/></param>
public sealed record LiveConnection(
    string ConnectionId,
    DateTimeOffset ConnectedAt
);
