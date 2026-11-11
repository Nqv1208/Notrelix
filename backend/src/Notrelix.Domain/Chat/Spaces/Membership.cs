using Notrelix.Domain.Chat.ReadStates;

namespace Notrelix.Domain.Chat.Spaces;

public enum MembershipRole
{
    Manager,
    Member,
}

public enum NotificationLevel
{
    All,
    Mentions,
    None,
}

/// <summary>
/// A person's place in one space. Owned by the Space aggregate: only the aggregate joins, leaves and counts
/// memberships, so the manager invariant is decided where the managers are known. Keyed by (space, user).
/// </summary>
public sealed class Membership : IWorkspaceScoped
{
    public Guid SpaceId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public MembershipRole Role { get; private set; }
    public NotificationLevel NotificationLevel { get; private set; } = NotificationLevel.All;
    public DateTimeOffset JoinedAt { get; private set; }
    public DateTimeOffset? LeftAt { get; private set; }

    /// <summary>The highest sequence the person has seen in the space (CHT-009). Moves forward only; see <see cref="ReadStates.ReadBoundary"/>.</summary>
    public long ReadSequence { get; private set; }

    /// <summary>Mentions of the person not yet read (CHT-010, S2.4). No mention facts exist in Phase 1, so this stays 0.</summary>
    public int MentionCount { get; private set; }

    /// <summary>When a mute ends. Only set with <see cref="NotificationLevel.None"/>; null is a permanent mute or no mute.</summary>
    public DateTimeOffset? MutedUntil { get; private set; }

    public bool IsActive => LeftAt is null;
    public bool IsManager => IsActive && Role == MembershipRole.Manager;

    private Membership() { }

    internal static Membership Create(Guid spaceId, Guid accountId, Guid workspaceId, Guid userId, MembershipRole role, DateTimeOffset joinedAt)
    {
        Guard.NotEmpty(spaceId);
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(userId);

        return new Membership
        {
            SpaceId = spaceId,
            AccountId = accountId,
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = role,
            JoinedAt = joinedAt,
        };
    }

    /// <summary>A person who left comes back as a fresh member; the row is reused, never duplicated.</summary>
    internal void Rejoin(MembershipRole role, DateTimeOffset at)
    {
        Role = role;
        JoinedAt = at;
        LeftAt = null;
    }

    internal void Leave(DateTimeOffset at) => LeftAt = at;

    /// <summary>
    /// Ends this membership because the person left or was removed from the Workspace that owns the space. Only the Workspace
    /// exit path calls it. Unlike <see cref="Space.RemoveMember"/> the last manager may go: the channel stays, without a manager,
    /// and Workspace Owners and Admins still manage it (ChannelAccess.RequireManager). Returns false when it already ended.
    /// </summary>
    public bool EndWorkspaceExit(DateTimeOffset leftAt)
    {
        if (!IsActive) return false;

        Leave(leftAt);
        return true;
    }

    /// <summary>
    /// Sets how the person is notified in this space (UC-CHT-D05, D06). A mute is the level None with an optional until.
    /// A level other than None clears the until. An until that is not after <paramref name="now"/> is a no-op. Returns
    /// false and changes nothing when the stored settings already match.
    /// </summary>
    public bool SetNotificationLevel(NotificationLevel level, DateTimeOffset? mutedUntil, DateTimeOffset now)
    {
        if (mutedUntil is not null && level != NotificationLevel.None)
            throw new ArgumentException("Only a muted space takes an until time.", nameof(mutedUntil));

        var until = mutedUntil?.ToUniversalTime();
        if (until is { } at && at <= now.ToUniversalTime())
            return false;

        if (NotificationLevel == level && MutedUntil == until)
            return false;

        NotificationLevel = level;
        MutedUntil = until;
        return true;
    }

    /// <summary>The level in force at <paramref name="now"/>: a mute whose until has passed is over, so the level is All.</summary>
    public NotificationLevel EffectiveNotificationLevel(DateTimeOffset now) =>
        EffectiveLevel(NotificationLevel, MutedUntil, now);

    /// <summary>
    /// Moves the read sequence to the target when it is ahead, never back (CHT-009). The store applies the same rule in one
    /// UPDATE for the concurrent path; this is the in-memory form, for a membership loaded and changed as a whole.
    /// </summary>
    public bool AdvanceReadSequence(long target)
    {
        var next = ReadBoundary.Advance(ReadSequence, target);
        if (next == ReadSequence) return false;

        ReadSequence = next;
        return true;
    }

    /// <summary>The level in force for stored settings at <paramref name="now"/> (see <see cref="EffectiveNotificationLevel"/>).</summary>
    public static NotificationLevel EffectiveLevel(NotificationLevel stored, DateTimeOffset? mutedUntil, DateTimeOffset now) =>
        stored == NotificationLevel.None && mutedUntil is { } until && until <= now.ToUniversalTime()
            ? NotificationLevel.All
            : stored;

    /// <summary>Muted at <paramref name="now"/>: no badge and no notification for the space (CHT-019).</summary>
    public bool IsMuted(DateTimeOffset now) => EffectiveNotificationLevel(now) == NotificationLevel.None;
}
