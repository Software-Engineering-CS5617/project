// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Screenshare.Streaming;

internal interface INetworkStreaming
{
    void StartSharing();
    void StopSharing();

    void Subscribe(string streamId);
    void Unsubscribe(string streamId);

    void UpdateSendToList(string streamId, List<string> sendToClients);
}
