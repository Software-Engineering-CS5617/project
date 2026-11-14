// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Cloud.Networking.Mocking;

/// <summary>
/// Abstraction over <see cref="Trace.WriteLine(string)"/> to be able to mock it.
/// </summary>
internal interface ITracer
{
    void WriteLine(string message);
}
