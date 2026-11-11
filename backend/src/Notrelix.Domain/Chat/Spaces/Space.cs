using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Chat.Spaces;

/// <summary>
/// A space: the unit of scope and access in Chat (CHT-001). It is a channel or a conversation (direct or group, S2.1). It owns
/// its memberships and the per-space sequence counter (<see cref="LastSequence"/>), which every message send advances.
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

    /// <summary>A direct or group conversation: a space whose members are fixed people, not a channel (CHT-003).</summary>
    public bool IsConversation => Kind is SpaceKind.Direct or SpaceKind.Group;

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

    /// <summary>
    /// Creates the direct conversation of two people (UC-CHT-E01). Both start as members; the name stays empty because a direct
    /// conversation has no name. The pair itself is recorded by <see cref="DirectPair"/>.
    /// </summary>
    public static Space CreateDirect(Guid accountId, Guid workspaceId, Guid createdBy, Guid otherUserId, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);
        Guard.NotEmpty(otherUserId);
        if (createdBy == otherUserId)
            throw new BusinessRuleException(ChatRuleCodes.SelfTarget, "A direct conversation needs two different people.");

        var space = new Space
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Kind = SpaceKind.Direct,
            Name = string.Empty,
            Position = 0,
            LastSequence = 0,
        };
        space.SetAuditOnCreate(createdBy, createdAt);
        space._memberships.Add(Membership.Create(space.Id, accountId, workspaceId, createdBy, MembershipRole.Member, createdAt));
        space._memberships.Add(Membership.Create(space.Id, accountId, workspaceId, otherUserId, MembershipRole.Member, createdAt));
        return space;
    }

    /// <summary>
    /// Creates a group conversation with its first participants (UC-CHT-E02). The caller is one of <paramref name="participantIds"/>.
    /// A group has at least three people and at most <see cref="ChatLimits.GroupCap"/>, counted with the caller; the name is
    /// optional (empty when not given). Duplicate ids count once.
    /// </summary>
    public static Space CreateGroup(
        Guid accountId,
        Guid workspaceId,
        string? name,
        IEnumerable<Guid> participantIds,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(createdBy);

        var people = participantIds.Distinct().ToList();
        if (!people.Contains(createdBy))
            people.Add(createdBy);
        if (people.Count < 3)
            throw new BusinessRuleException(ChatRuleCodes.GroupTooSmall, "A group conversation has at least three people.");
        if (people.Count > ChatLimits.GroupCap)
            throw new BusinessRuleException(ChatRuleCodes.LimitReached, $"A group conversation holds at most {ChatLimits.GroupCap} people.");

        var space = new Space
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Kind = SpaceKind.Group,
            Name = NormalizeConversationName(name),
            Position = 0,
            LastSequence = 0,
        };
        space.SetAuditOnCreate(createdBy, createdAt);
        foreach (var person in people)
            space._memberships.Add(Membership.Create(space.Id, accountId, workspaceId, person, MembershipRole.Member, createdAt));
        return space;
    }

    /// <summary>
    /// Adds a participant to a group conversation (UC-CHT-E03). Returns false when the person is already a participant. The group
    /// holds at most <see cref="ChatLimits.GroupCap"/> active participants; the next one is refused with limit-reached.
    /// </summary>
    public bool AddParticipant(Guid userId, DateTimeOffset joinedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(userId);
        if (Kind != SpaceKind.Group)
            throw new BusinessRuleException(ChatRuleCodes.KindNotSupported, "Only a group conversation takes new participants.");
        if (IsActiveMember(userId)) return false;
        if (ActiveMemberCount() >= ChatLimits.GroupCap)
            throw new BusinessRuleException(ChatRuleCodes.LimitReached, $"A group conversation holds at most {ChatLimits.GroupCap} people.");

        return AddMember(userId, MembershipRole.Member, joinedAt);
    }

    /// <summary>
    /// The caller leaves a group conversation (UC-CHT-E04). Returns false when they are not a participant. A direct conversation
    /// cannot be left: its two people stay for good (400 kind-not-supported).
    /// </summary>
    public bool LeaveConversation(Guid userId, DateTimeOffset leftAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(userId);
        if (Kind != SpaceKind.Group)
            throw new BusinessRuleException(ChatRuleCodes.KindNotSupported, "Only a group conversation can be left.");

        var member = GetActiveMembership(userId);
        if (member is null) return false;

        member.Leave(leftAt);
        return true;
    }

    /// <summary>Closes a conversation that has no active participant left (UC-CHT-E04): a soft delete, purged by retention.</summary>
    public bool CloseIfNoParticipants(Guid closedBy, DateTimeOffset closedAt)
    {
        if (IsDeleted || ActiveMemberCount() > 0) return false;

        Delete(closedBy, closedAt);
        return true;
    }

    /// <summary>The number of people who are active members now.</summary>
    public int ActiveMemberCount() => _memberships.Count(m => m.IsActive);

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

    /// <summary>A conversation name is optional: empty when not given, otherwise trimmed and at most the channel name length.</summary>
    private static string NormalizeConversationName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var trimmed = name!.Trim();
        Guard.MaxLength(trimmed, ChatLimits.ChannelNameMaxLength);
        return trimmed;
    }

    private static void EnsureFinite(double position)
    {
        if (!double.IsFinite(position))
            throw new BusinessRuleException(ChatRuleCodes.InvalidPosition, "Position must be a finite number.");
    }
}
