using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Chat.Spaces;

/// <summary>
/// A space: the unit of scope and access in Chat (CHT-001). Phase 1 creates channels. It owns its memberships
/// and the per-space sequence counter (<see cref="LastSequence"/>), which message sends advance in a later slice.
/// </summary>
/// <remarks>
/// Invariants held here: the name is 1 to 80 characters; an archived channel is read-only (every change except
/// archive, unarchive and delete is refused); a live channel keeps at least one manager. The name is unique per
/// Workspace among active channels; that rule needs the other channels, so the handler checks it and the
/// database enforces it with a partial unique index.
/// </remarks>
public class Space : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    private readonly List<Membership> _memberships = [];

    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public SpaceKind Kind { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Topic { get; private set; }
    public Guid? GroupId { get; private set; }
    public double Position { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    /// <summary>The sequence of the last message stored in the space. Starts at 0; system messages (S1.2) advance it.</summary>
    public long LastSequence { get; private set; }

    public IReadOnlyCollection<Membership> Memberships => _memberships.AsReadOnly();

    public bool IsArchived => ArchivedAt is not null;
    public bool IsPublic => Kind == SpaceKind.ChannelPublic;
    public bool IsPrivate => Kind == SpaceKind.ChannelPrivate;

    private Space() : base() { }

    public static Space CreateChannel(
        Guid accountId,
        Guid workspaceId,
        SpaceKind kind,
        string name,
        string? topic,
        Guid? groupId,
        double position,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);
        if (kind is not (SpaceKind.ChannelPublic or SpaceKind.ChannelPrivate))
            throw new BusinessRuleException(ChatRuleCodes.KindNotSupported, "Only public and private channels can be created in this phase.");
        EnsureFinite(position);

        var space = new Space
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Kind = kind,
            Name = NormalizeName(name),
            Topic = NormalizeTopic(topic),
            GroupId = groupId,
            Position = position,
            LastSequence = 0,
        };
        space.SetAuditOnCreate(createdBy, createdAt);
        space._memberships.Add(Membership.Create(space.Id, accountId, workspaceId, createdBy, MembershipRole.Manager, createdAt));
        return space;
    }

    /// <summary>Renames and sets the topic. Returns false, and changes nothing, when both are already as given.</summary>
    public bool UpdateDetails(string name, string? topic, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(updatedBy);

        var normalizedName = NormalizeName(name);
        var normalizedTopic = NormalizeTopic(topic);
        if (normalizedName == Name && normalizedTopic == Topic) return false;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = normalizedName;
        Topic = normalizedTopic;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        return true;
    }

    /// <summary>Archives or unarchives. Archiving is allowed on a live channel; unarchiving is the way back.</summary>
    public bool SetArchived(bool archived, Guid changedBy, DateTimeOffset changedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(changedBy);
        if (archived == IsArchived) return false;

        var pending = PrepareAuditUpdate(changedBy, changedAt);
        ArchivedAt = archived ? changedAt : null;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        return true;
    }

    /// <summary>Soft delete. The recovery window and the purge are a later rule (CHT-018).</summary>
    public void Delete(Guid deletedBy, DateTimeOffset deletedAt)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;

        var pending = PrepareDeletion(deletedBy, deletedAt, reason: null);
        ApplyDeletion(pending);
        IncrementVersion();
    }

    /// <summary>Moves the channel in the sidebar. Returns false when it already sits there.</summary>
    public bool MoveTo(Guid? groupId, double position, Guid movedBy, DateTimeOffset movedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(movedBy);
        EnsureFinite(position);
        if (GroupId == groupId && Position == position) return false;

        var pending = PrepareAuditUpdate(movedBy, movedAt);
        GroupId = groupId;
        Position = position;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        return true;
    }

    /// <summary>Clears the group of this channel when the group is deleted.</summary>
    public bool RemoveFromGroup(Guid removedBy, DateTimeOffset removedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(removedBy);
        if (GroupId is null) return false;

        var pending = PrepareAuditUpdate(removedBy, removedAt);
        GroupId = null;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        return true;
    }

    /// <summary>
    /// Adds a person as a member, or brings a former member back. Returns false when the person is already a
    /// member. Who may add whom is decided by the caller; this method only holds the state rules.
    /// </summary>
    public bool AddMember(Guid userId, MembershipRole role, DateTimeOffset joinedAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(userId);

        var existing = _memberships.SingleOrDefault(m => m.UserId == userId);
        if (existing is { IsActive: true }) return false;

        if (existing is null)
            _memberships.Add(Membership.Create(Id, AccountId, WorkspaceId, userId, role, joinedAt));
        else
            existing.Rejoin(role, joinedAt);
        return true;
    }

    /// <summary>
    /// Ends a membership: a leave or a removal. Returns false when the person is not an active member. A live
    /// channel keeps at least one manager, so the last one cannot go.
    /// </summary>
    public bool RemoveMember(Guid userId, DateTimeOffset leftAt)
    {
        EnsureNotDeleted();
        EnsureNotArchived();
        Guard.NotEmpty(userId);

        var member = GetActiveMembership(userId);
        if (member is null) return false;

        if (member.Role == MembershipRole.Manager && ActiveManagerCount() <= 1)
            throw new BusinessRuleException(ChatRuleCodes.LastManager, "A channel keeps at least one manager.");

        member.Leave(leftAt);
        return true;
    }

    public Membership? GetActiveMembership(Guid userId) =>
        _memberships.SingleOrDefault(m => m.UserId == userId && m.IsActive);

    public bool IsActiveMember(Guid userId) => GetActiveMembership(userId) is not null;

    public bool IsActiveManager(Guid userId) => GetActiveMembership(userId) is { Role: MembershipRole.Manager };

    public int ActiveManagerCount() => _memberships.Count(m => m.IsManager);

    private void EnsureNotArchived()
    {
        if (IsArchived)
            throw new BusinessRuleException(ChatRuleCodes.ChannelArchived, "The channel is archived and read-only.");
    }

    private static string NormalizeName(string? name)
    {
        Guard.NotNullOrWhiteSpace(name);
        var trimmed = name!.Trim();
        Guard.MaxLength(trimmed, ChatLimits.ChannelNameMaxLength);
        return trimmed;
    }

    private static string? NormalizeTopic(string? topic)
    {
        if (string.IsNullOrWhiteSpace(topic)) return null;
        var trimmed = topic.Trim();
        Guard.MaxLength(trimmed, ChatLimits.ChannelTopicMaxLength);
        return trimmed;
    }

    private static void EnsureFinite(double position)
    {
        if (!double.IsFinite(position))
            throw new BusinessRuleException(ChatRuleCodes.InvalidPosition, "Position must be a finite number.");
    }
}
