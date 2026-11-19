using Notrelix.Domain.Chat.Calls.Events;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Domain.Chat.Calls;

/// <summary>
/// A time-bounded voice or video session attached to a space (SPEC-CHAT-MEDIA-CALLS sections 4 and 5, SYS-ADR-002 decision 6).
/// States: Ringing, Active, Ended (final, with a reason). Every change that matters to a client bumps <see cref="AggregateRoot.Version"/>
/// and raises one <see cref="CallChangedDomainEvent"/>; a command that changes nothing (a repeat) changes no version and raises none.
/// A direct call has two people and ends when either one ends or leaves it; a group or channel call lives on while anyone is in it.
/// </summary>
public sealed class Call : AggregateRoot, IWorkspaceScoped
{
    private readonly List<CallParticipant> _participants = [];

    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid SpaceId { get; private set; }
    public SpaceKind SpaceKind { get; private set; }
    public Guid InitiatorId { get; private set; }
    public CallKind Kind { get; private set; }
    public CallState State { get; private set; }
    public CallEndReason? EndReason { get; private set; }

    /// <summary>The random name of the media room; never the id of anything inside Notrelix.</summary>
    public string RoomKey { get; private set; } = null!;

    /// <summary>The media host region the room was created in. The server sets it from configuration; a client never supplies it.</summary>
    public string Region { get; private set; } = null!;

    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? AnsweredAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>When the last person left an Active call; the sweep ends the call after the grace period. Null while anyone is in it.</summary>
    public DateTimeOffset? EmptySince { get; private set; }

    public IReadOnlyCollection<CallParticipant> Participants => _participants.AsReadOnly();

    public bool IsDirect => SpaceKind == SpaceKind.Direct;
    public bool IsEnded => State == CallState.Ended;

    /// <summary>The people who ring: a direct or group conversation rings its members; a channel call shows a banner and does not ring.</summary>
    public bool RingsMembers => SpaceKind is SpaceKind.Direct or SpaceKind.Group;

    public int JoinedCount => _participants.Count(p => p.IsJoined);

    private Call() : base() { }

    public static int MaxParticipants(SpaceKind spaceKind, CallKind kind) => spaceKind == SpaceKind.Direct
        ? ChatLimits.CallMaxParticipantsDirect
        : kind == CallKind.Video ? ChatLimits.CallMaxParticipantsVideo : ChatLimits.CallMaxParticipantsVoice;

    /// <summary>A ringing call; the initiator is its first participant and holds the first join token.</summary>
    public static Call Start(
        Guid accountId, Guid workspaceId, Guid spaceId, SpaceKind spaceKind, Guid initiatorId, CallKind kind,
        string roomKey, string region, DateTimeOffset at)
    {
        var call = Create(accountId, workspaceId, spaceId, spaceKind, initiatorId, kind, roomKey, region, at);
        call._participants.Add(CallParticipant.Join(call.Id, accountId, workspaceId, initiatorId, at));
        call.Raise(initiatorId, at);
        return call;
    }

    /// <summary>A call that never rang because the other person was in another call: stored as Ended(Busy) for the history (invariant 2).</summary>
    public static Call StartBusy(
        Guid accountId, Guid workspaceId, Guid spaceId, SpaceKind spaceKind, Guid initiatorId, CallKind kind,
        string roomKey, string region, DateTimeOffset at)
    {
        var call = Create(accountId, workspaceId, spaceId, spaceKind, initiatorId, kind, roomKey, region, at);
        call.State = CallState.Ended;
        call.EndReason = CallEndReason.Busy;
        call.EndedAt = at;
        call.Raise(initiatorId, at);
        return call;
    }

    private static Call Create(
        Guid accountId, Guid workspaceId, Guid spaceId, SpaceKind spaceKind, Guid initiatorId, CallKind kind,
        string roomKey, string region, DateTimeOffset at)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(spaceId);
        Guard.NotEmpty(initiatorId);
        Guard.NotNullOrWhiteSpace(roomKey);
        Guard.NotNullOrWhiteSpace(region);

        var call = new Call
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            SpaceId = spaceId,
            SpaceKind = spaceKind,
            InitiatorId = initiatorId,
            Kind = kind,
            State = CallState.Ringing,
            RoomKey = roomKey,
            Region = region,
            StartedAt = at,
        };
        call.SetAuditOnCreate(initiatorId, at);
        return call;
    }

    /// <summary>
    /// A person other than the initiator answers: Ringing becomes Active. A repeat by someone already in the call changes nothing
    /// (false). Any other state is refused.
    /// </summary>
    public bool Answer(Guid personId, DateTimeOffset at)
    {
        EnsureNotEnded();
        if (IsJoined(personId)) return false;
        if (State != CallState.Ringing)
            throw new BusinessRuleException(ChatRuleCodes.CallStateInvalid, "Only a ringing call can be answered.");

        Admit(personId, at);
        State = CallState.Active;
        AnsweredAt = at;
        EmptySince = null;
        Touch(personId, at);
        return true;
    }

    /// <summary>A group or channel member joins a live call (a ringing call becomes active, as an answer does). A repeat changes nothing.</summary>
    public bool Join(Guid personId, DateTimeOffset at)
    {
        EnsureNotEnded();
        if (IsJoined(personId)) return false;
        if (State == CallState.Ringing) return Answer(personId, at);

        Admit(personId, at);
        EmptySince = null;
        Touch(personId, at);
        return true;
    }

    /// <summary>The called person declines. A direct call ends as Declined; a group or channel call keeps ringing for the others (no change, false).</summary>
    public bool Decline(Guid personId, DateTimeOffset at)
    {
        EnsureNotEnded();
        if (State != CallState.Ringing)
            throw new BusinessRuleException(ChatRuleCodes.CallStateInvalid, "Only a ringing call can be declined.");
        if (personId == InitiatorId)
            throw new BusinessRuleException(ChatRuleCodes.CallNotParticipant, "The person who started the call cancels it instead.");
        if (!IsDirect) return false;

        Finish(CallEndReason.Declined, personId, at);
        return true;
    }

    /// <summary>The initiator hangs up before anyone answered.</summary>
    public void Cancel(Guid personId, DateTimeOffset at)
    {
        EnsureNotEnded();
        if (State != CallState.Ringing)
            throw new BusinessRuleException(ChatRuleCodes.CallStateInvalid, "Only a ringing call can be cancelled.");
        if (personId != InitiatorId)
            throw new BusinessRuleException(ChatRuleCodes.CallNotParticipant, "Only the person who started the call cancels it.");

        Finish(CallEndReason.Cancelled, personId, at);
    }

    /// <summary>Nobody answered in time (the sweep, after <c>Chat:Calls:RingSeconds</c>).</summary>
    public void Miss(DateTimeOffset at)
    {
        EnsureNotEnded();
        if (State != CallState.Ringing)
            throw new BusinessRuleException(ChatRuleCodes.CallStateInvalid, "Only a ringing call can be missed.");

        Finish(CallEndReason.Missed, InitiatorId, at);
    }

    /// <summary>The media plane failed (reconciliation). Ringing or Active becomes Ended(Failed).</summary>
    public void Fail(DateTimeOffset at)
    {
        EnsureNotEnded();
        Finish(CallEndReason.Failed, InitiatorId, at);
    }

    /// <summary>
    /// Ends an Active call for everyone. In a direct call either person may end it; in a group or channel call only the initiator does
    /// (the others leave).
    /// </summary>
    public void End(Guid personId, DateTimeOffset at)
    {
        EnsureNotEnded();
        if (State != CallState.Active)
            throw new BusinessRuleException(ChatRuleCodes.CallStateInvalid, "Only an active call can be ended; a ringing call is cancelled or declined.");
        if (IsDirect ? !IsJoined(personId) : personId != InitiatorId)
            throw new BusinessRuleException(ChatRuleCodes.CallNotParticipant, "You may not end this call.");

        Finish(CallEndReason.Completed, personId, at);
    }

    /// <summary>
    /// A participant leaves. Leaving a direct call ends it (two people); leaving a group or channel call keeps it alive, and the sweep
    /// ends it when nobody is left for the grace period. A person who is not in the call changes nothing (false).
    /// </summary>
    public bool Leave(Guid personId, DateTimeOffset at)
    {
        EnsureNotEnded();
        var participant = Find(personId);
        if (participant is not { IsJoined: true }) return false;
        if (State != CallState.Active)
            throw new BusinessRuleException(ChatRuleCodes.CallStateInvalid, "Only an active call is left; a ringing call is cancelled or declined.");

        if (IsDirect)
        {
            Finish(CallEndReason.Completed, personId, at);
            return true;
        }

        participant.Leave(at);
        if (JoinedCount == 0) EmptySince = at;
        Touch(personId, at);
        return true;
    }

    /// <summary>
    /// The person's access to the space ended while they were in the call: they are released as a cancel (starter of a ringing call), a leave
    /// (a direct call ends) or, when that leaves nobody in an active call, an end. One fact asks the media room to drop them. False when
    /// they were not in the call.
    /// </summary>
    public bool Revoke(Guid personId, DateTimeOffset at)
    {
        if (IsEnded || !IsJoined(personId)) return false;

        if (State == CallState.Ringing)
            Cancel(personId, at); // only the starter is in a ringing call
        else
        {
            Leave(personId, at);
            if (!IsEnded && JoinedCount == 0) EndEmpty(at);
        }

        RaiseDomainEvent(new CallParticipantRevokedDomainEvent(AccountId, WorkspaceId, SpaceId, Id, personId, RoomKey, at));
        return true;
    }

    /// <summary>
    /// The media server reports the room finished (the webhook). A Ringing call never got its room used, so it ends as Failed; an Active call
    /// ends as Completed for everyone. An ended call changes nothing (false).
    /// </summary>
    public bool EndFromMedia(DateTimeOffset at)
    {
        if (IsEnded) return false;
        Finish(State == CallState.Ringing ? CallEndReason.Failed : CallEndReason.Completed, InitiatorId, at);
        return true;
    }

    /// <summary>
    /// A participant's right to publish changed while they are in the call (a channel turned read-only). One fact asks the media room to
    /// apply it; it changes no stored call state, so the version stays. False when they are not in the call.
    /// </summary>
    public bool ChangePublishPermission(Guid personId, bool canPublish, DateTimeOffset at)
    {
        if (IsEnded || !IsJoined(personId)) return false;
        RaiseDomainEvent(new CallParticipantPermissionChangedDomainEvent(
            AccountId, WorkspaceId, SpaceId, Id, personId, RoomKey, canPublish, canPublish && Kind == CallKind.Video, at));
        return true;
    }

    /// <summary>The sweep ends an Active call that has had nobody in it for the grace period.</summary>
    public void EndEmpty(DateTimeOffset at)
    {
        EnsureNotEnded();
        if (State != CallState.Active || JoinedCount > 0)
            throw new BusinessRuleException(ChatRuleCodes.CallStateInvalid, "Only an empty active call ends this way.");

        Finish(CallEndReason.Completed, InitiatorId, at);
    }

    /// <summary>
    /// A participant's own microphone, camera and screen share. A voice call carries no camera and no screen (the media token grants only the
    /// microphone there). Only one person shares at a time: starting a share while another person shares is refused unless
    /// <paramref name="replaceShare"/> says the caller confirmed it, and then the other person's share ends in the same change. Same values
    /// change nothing (false).
    /// </summary>
    public bool UpdateMediaState(Guid personId, bool audioOn, bool videoOn, bool sharingScreen, DateTimeOffset at, bool replaceShare = false)
    {
        EnsureNotEnded();
        var participant = Find(personId);
        if (participant is not { IsJoined: true })
            throw new BusinessRuleException(ChatRuleCodes.CallNotParticipant, "You are not in this call.");
        if (videoOn && Kind == CallKind.Voice)
            throw new BusinessRuleException(ChatRuleCodes.CallMediaNotAllowed, "A voice call carries no camera.");
        if (sharingScreen && Kind == CallKind.Voice)
            throw new BusinessRuleException(ChatRuleCodes.CallMediaNotAllowed, "A voice call carries no screen share.");

        var changed = false;
        if (sharingScreen && !participant.SharingScreen)
        {
            var current = _participants.Where(p => p.IsJoined && p.SharingScreen && p.PersonId != personId).ToList();
            if (current.Count > 0 && !replaceShare)
                throw new BusinessRuleException(ChatRuleCodes.CallScreenShareTaken, "Someone else is already sharing a screen.");
            foreach (var other in current) changed |= other.StopSharing();
        }

        changed |= participant.SetMedia(audioOn, videoOn, sharingScreen);
        if (!changed) return false;
        Touch(personId, at);
        return true;
    }

    public bool IsJoined(Guid personId) => Find(personId) is { IsJoined: true };

    private CallParticipant? Find(Guid personId) => _participants.FirstOrDefault(p => p.PersonId == personId);

    private void Admit(Guid personId, DateTimeOffset at)
    {
        if (JoinedCount >= MaxParticipants(SpaceKind, Kind))
            throw new BusinessRuleException(ChatRuleCodes.CallFull, "The call is full.");

        var existing = Find(personId);
        if (existing is null) _participants.Add(CallParticipant.Join(Id, AccountId, WorkspaceId, personId, at));
        else existing.Rejoin(at);
    }

    private void Finish(CallEndReason reason, Guid actorId, DateTimeOffset at)
    {
        foreach (var participant in _participants.Where(p => p.IsJoined)) participant.Leave(at);
        State = CallState.Ended;
        EndReason = reason;
        EndedAt = at;
        EmptySince = null;
        Touch(actorId, at);
    }

    private void EnsureNotEnded()
    {
        if (IsEnded)
            throw new BusinessRuleException(ChatRuleCodes.CallEnded, "The call has ended.");
    }

    private void Touch(Guid actorId, DateTimeOffset at)
    {
        var pending = PrepareAuditUpdate(actorId, at);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        Raise(actorId, at);
    }

    private void Raise(Guid actorId, DateTimeOffset at) =>
        RaiseDomainEvent(new CallChangedDomainEvent(
            AccountId, WorkspaceId, SpaceId, Id, InitiatorId, actorId, Kind, State, EndReason, RingsMembers, Version,
            _participants.Where(p => p.IsJoined).Select(p => p.PersonId).ToArray(), at));
}
