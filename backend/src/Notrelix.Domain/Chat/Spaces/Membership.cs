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
}
