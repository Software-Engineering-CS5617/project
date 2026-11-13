// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Decorators;
using Cloud.Networking.Mocking;

namespace Cloud.Networking;

/// <summary>
/// So that there is single point of access.
/// Outer modules don't know the implementation, they can just see the interface.
/// </summary>
public static class MessengerFactory
{
    /// <summary>
    /// A SignalR messenger with diagnostic tracing but exceptions propagate to the caller.
    /// </summary>
    public static IMessenger GetMessenger(string serverUrl)
    {
        return new DiagnosticMessenger(new SignalRMessenger(serverUrl), new TracerProxy());
    }

    /// <summary>
    /// Same as <see cref="GetMessenger"/>, but exceptions are swallowed.
    /// </summary>
    public static IMessenger GetSilentMessenger(string serverUrl)
    {
        return new SilentMessenger(GetMessenger(serverUrl));
    }
}
