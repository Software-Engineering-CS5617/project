// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ScreenShare.Interfaces;

public interface IScreenShareFeed
{
    event Action<string, string>? ParticipantJoined; // id, name 
    event Action<string>? ParticipantLeft;  // id 
    event Action<string, byte[]>? FrameReady; //Id, stitched images bytes
    void RequestFeeds(IReadOnlyDictionary<string, string> idToResolution); //Who is visible at what quality
    void RequestFeeds(Dictionary<string, int> dictionary);
}
