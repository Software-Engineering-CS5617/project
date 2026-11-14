// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Cloud.Networking.Mocking;

namespace Cloud.Test;

/// <summary>
/// Tests the TracerProxy class
/// </summary>
[TestClass]
public class TestTracerProxy
{
    private sealed class CapturingListener : TraceListener
    {
        public ConcurrentQueue<string> Lines { get; } = new();

        public override void Write(string? message)
        {
        }

        public override void WriteLine(string? message)
        {
            Lines.Enqueue(message ?? string.Empty);
        }
    }

    /// <summary>Tests that WriteLine forwards the message to the registered trace listeners.</summary>
    [TestMethod]
    public void WriteLineForwardsToTraceListeners()
    {
        CapturingListener listener = new();
        Trace.Listeners.Add(listener);

        try
        {
            string message = $"tracer-proxy-test-{Guid.NewGuid()}";

            new TracerProxy().WriteLine(message);

            Assert.IsTrue(listener.Lines.Contains(message));
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }
    }
}
