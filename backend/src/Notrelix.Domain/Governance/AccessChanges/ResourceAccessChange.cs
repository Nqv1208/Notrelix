namespace Notrelix.Domain.Governance.AccessChanges;

/// <summary>What happened to who can reach a resource.</summary>
public enum ResourceAccessChangeKind
{
    /// <summary>A person or Team was given access (explicit grant or board membership).</summary>
    Added,

    /// <summary>The level of a person or Team changed.</summary>
    Changed,

    /// <summary>A person or Team lost its explicit access.</summary>
    Removed,

    /// <summary>The resource was made Private.</summary>
    MadePrivate,

    /// <summary>The resource's visibility changed to something other than Private.</summary>
    VisibilityChanged
}

/// <summary>
/// One append-only record of an access change on a resource, kept by Governance for the sharing log of a
/// Board (managers read it). It is not the enterprise audit trail: it is a bounded, workspace-scoped
/// read source with its own retention (<see cref="RetentionUntil"/>). A subject is named at read time.
/// </summary>
public sealed class ResourceAccessChange : Entity, IWorkspaceScoped
{
    /// <summary>The log keeps a change for 90 days.</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromDays(90);

    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceKind ResourceKind { get; private set; }
    public Guid ResourceId { get; private set; }
    public ResourceAccessChangeKind Kind { get; private set; }
    public Guid ActorUserId { get; private set; }

    /// <summary><c>User</c> or <c>Team</c>; null for a visibility change.</summary>
    public string? SubjectType { get; private set; }
    public Guid? SubjectId { get; private set; }

    /// <summary>Level token before the change (viewer, commenter, editor, manager); null when none.</summary>
    public string? FromLevel { get; private set; }

    /// <summary>Level token after the change; null when none.</summary>
    public string? ToLevel { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset RetentionUntil { get; private set; }

    private ResourceAccessChange() : base() { }

    public static ResourceAccessChange Record(
        Guid accountId,
        Guid workspaceId,
        ResourceKind resourceKind,
        Guid resourceId,
        ResourceAccessChangeKind kind,
        Guid actorUserId,
        string? subjectType,
        Guid? subjectId,
        string? fromLevel,
        string? toLevel,
        DateTimeOffset occurredAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(resourceId);
        Guard.NotEmpty(actorUserId);

        var isSubjectChange = kind is ResourceAccessChangeKind.Added or ResourceAccessChangeKind.Changed or ResourceAccessChangeKind.Removed;
        if (isSubjectChange)
        {
            if (subjectType is not ("User" or "Team") || subjectId is null || subjectId == Guid.Empty)
                throw new BusinessRuleException(
                    GovernanceRuleCodes.Governance_AccessChange_SubjectRequired,
                    "An access change to a person or Team needs its subject.");
        }
        else
        {
            subjectType = null;
            subjectId = null;
        }

        return new ResourceAccessChange
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ResourceKind = resourceKind,
            ResourceId = resourceId,
            Kind = kind,
            ActorUserId = actorUserId,
            SubjectType = subjectType,
            SubjectId = subjectId,
            FromLevel = fromLevel,
            ToLevel = toLevel,
            OccurredAt = occurredAt,
            RetentionUntil = occurredAt + Retention
        };
    }
}
