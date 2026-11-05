using System;
using System.Collections.Generic;
using SecureMeet.Shared.Models;

namespace SecureMeet.Shared.Modules;

// What every module gets in Initialize: who is logged in and what meeting is running.
// All events come on the UI thread.
public interface IMeetingSession
{
    UserProfile? CurrentUser { get; }               // null when nobody is logged in
    IReadOnlyList<UserProfile> Users { get; }       // everyone who logged in on this computer
    MeetingInfo? Meeting { get; }                   // null when there is no meeting
    bool InMeeting { get; }

    // Controller team: set this when someone joins or leaves (any thread is fine).
    int ParticipantCount { get; set; }

    event Action<UserProfile>? UserSignedIn;
    event Action? UserSignedOut;
    event Action<UserProfile>? ProfileChanged;      // user changed their User ID
    event Action<MeetingInfo>? MeetingStarted;
    event Action? MeetingEnded;                     // also when the user signs out in a meeting
    event Action<PanelKind, bool>? PanelVisibilityChanged;
    event Action<int>? ParticipantCountChanged;
}
