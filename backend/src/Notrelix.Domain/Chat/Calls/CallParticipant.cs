namespace Notrelix.Domain.Chat.Calls;

/// <summary>
/// A person in one call. Owned by the Call aggregate: only the aggregate admits, releases and changes participants, so the roster
/// rules are decided where the roster is known. Keyed by (call, person); a person who leaves and comes back reuses the row.
/// </summary>
public sealed class CallParticipant : IWorkspaceScoped
{
    public Guid CallId { get; private set; }
    public Guid PersonId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
    public DateTimeOffset? LeftAt { get; private set; }
    public bool AudioOn { get; private set; }
    public bool VideoOn { get; private set; }
    public bool SharingScreen { get; private set; }

    public bool IsJoined => LeftAt is null;

    private CallParticipant() { }

    internal static CallParticipant Join(Guid callId, Guid accountId, Guid workspaceId, Guid personId, DateTimeOffset at)
    {
        Guard.NotEmpty(callId);
        Guard.NotEmpty(personId);
        return new CallParticipant
        {
            CallId = callId,
            AccountId = accountId,
            WorkspaceId = workspaceId,
            PersonId = personId,
            JoinedAt = at,
            AudioOn = true,
        };
    }

    internal void Rejoin(DateTimeOffset at)
    {
        JoinedAt = at;
        LeftAt = null;
        AudioOn = true;
        VideoOn = false;
        SharingScreen = false;
    }

    /// <summary>A person who left shares nothing.</summary>
    internal void Leave(DateTimeOffset at)
    {
        LeftAt = at;
        SharingScreen = false;
    }

    internal bool StopSharing()
    {
        if (!SharingScreen) return false;
        SharingScreen = false;
        return true;
    }

    internal bool SetMedia(bool audioOn, bool videoOn, bool sharingScreen)
    {
        if (AudioOn == audioOn && VideoOn == videoOn && SharingScreen == sharingScreen) return false;
        AudioOn = audioOn;
        VideoOn = videoOn;
        SharingScreen = sharingScreen;
        return true;
    }
}
