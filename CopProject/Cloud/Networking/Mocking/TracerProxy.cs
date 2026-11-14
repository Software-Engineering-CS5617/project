// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Cloud.Networking.Mocking;

/// <summary>
/// Pass-through Proxy which forwards to <see cref="Trace.WriteLine(string)"/>
/// </summary>
internal sealed class TracerProxy : ITracer
{
    public void WriteLine(string message)
    {
        Trace.WriteLine(message);
    }
}
