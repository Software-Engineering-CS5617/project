// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking;

/// <summary>
/// Hub method names shared by the client and the server.
/// </summary>
public static class Contract
{
    public const string SendMessage = nameof(SendMessage);
    public const string ReceiveMessage = nameof(ReceiveMessage);
}
