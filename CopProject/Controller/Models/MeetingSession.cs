using System;
using System.Collections.Generic;
using System.Threading;

namespace Controller.Models;

public sealed class MeetingSession
{
    readonly SynchronizationContext? uiThread = SynchronizationContext.Current;
    int participantCount = 1;

    public MeetingSession() { }

    public string CurrentUserId { get; set; } = string.Empty;
    public string CurrentUserEmail { get; set; } = string.Empty;

    public MeetingInfo? Meeting { get; private set; }
    public bool InMeeting => Meeting != null;

    public int ParticipantCount
    {
        get => participantCount;
        set
        {
            participantCount = Math.Max(0, value);
            if (uiThread != null) uiThread.Post(_ => ParticipantCountChanged?.Invoke(participantCount), null);
            else ParticipantCountChanged?.Invoke(participantCount);
        }
    }

    private IReadOnlyList<ParticipantInfo> _activeParticipants = Array.Empty<ParticipantInfo>();
    public IReadOnlyList<ParticipantInfo> ActiveParticipants
    {
        get => _activeParticipants;
        set
        {
            _activeParticipants = value ?? Array.Empty<ParticipantInfo>();
            if (uiThread != null) uiThread.Post(_ => ParticipantsChanged?.Invoke(_activeParticipants), null);
            else ParticipantsChanged?.Invoke(_activeParticipants);
        }
    }

    public event Action<MeetingInfo>? MeetingStarted;
    public event Action? MeetingEnded;
    public event Action<int>? ParticipantCountChanged;
    public event Action<IReadOnlyList<ParticipantInfo>>? ParticipantsChanged;
    public event Action? Changed;

    public void StartMeeting(MeetingInfo info)
    {
        Meeting = info;
        participantCount = 1;
        Changed?.Invoke();
        MeetingStarted?.Invoke(info);
    }

    public void EndMeeting()
    {
        if (Meeting == null) return;
        Meeting = null;
        _activeParticipants = Array.Empty<ParticipantInfo>();
        ParticipantsChanged?.Invoke(_activeParticipants);
        MeetingEnded?.Invoke();
        Changed?.Invoke();
    }
}