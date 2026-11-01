namespace Notrelix.Application.Common.Security;

public sealed record AccessFacts(
    bool UserExists,
    bool EmailVerified,
    bool AccountExists,
    string? AccountMemberRole,
    bool WorkspaceExists,
    string? WorkspaceMemberRole,
    bool ResourceExists,
    string? ResourceAudience,
    string? ResourceMemberRole,
    bool HasExplicitResourcePermission,
    IReadOnlyList<AccessPermissionRule> PermissionRules,
    bool SubscriptionRequirementSatisfied,
    bool FeatureEnabled,
    int? ActiveResourcePermissionRank,
    int? TargetPermissionRank)
{
    /// <summary>
    /// The Workspaces Space the evaluated resource is (or, for a Space resource,
    /// is) placed in; null when the resource is not placed in a Space (ADR-009).
    /// </summary>
    public Guid? ResourceSpaceId { get; init; }

    /// <summary>The target Space exists in the request Workspace and is not deleted.</summary>
    public bool SpaceExists { get; init; }

    /// <summary>
    /// The principal's effective Space role on the target Space from User and
    /// active Team entries on the Space or an ancestor (Admin | Member | Viewer),
    /// or null. Private descendants stop Member/Viewer inheritance; Admin passes.
    /// </summary>
    public string? SpaceRole { get; init; }

    /// <summary>No Private Space lies on the path from the Workspace root to the target Space.</summary>
    public bool SpaceIsOpen { get; init; }

    /// <summary>
    /// For a Board child resource (Item, Group, Field, View, Label, Checklist,
    /// Checklist item), the Board it is decided on (GOV-033): the resource,
    /// audience, membership, grant and Space facts above are then the Board's.
    /// Null for any other resource.
    /// </summary>
    public Guid? ParentBoardId { get; init; }

    /// <summary>
    /// The resource is of a Board child kind, whether or not its Board could be
    /// resolved; a Board child without <see cref="ParentBoardId"/> is NotFound.
    /// </summary>
    public bool IsBoardChild { get; init; }

    /// <summary>
    /// The board membership role of the request's target user on the Board the
    /// request is decided on (see <see cref="IRequirePermissionTarget"/>), or null.
    /// </summary>
    public string? TargetBoardMemberRole { get; init; }

    /// <summary>
    /// The Board the request is decided on (the Board itself, or the parent Board of a
    /// Board child) is archived. An archived Board stays reachable (<see cref="ResourceExists"/>);
    /// the policy refuses writes on it once the normal decision is Allow.
    /// </summary>
    public bool BoardArchived { get; init; }

    /// <summary>
    /// The Board is deleted and the request is one that may reach a deleted Board (restore, delete).
    /// A caller who then lacks the manager level gets the identical NotFound, so a deleted Board
    /// is never told apart from one that does not exist.
    /// </summary>
    public bool BoardDeleted { get; init; }

    /// <summary>
    /// The resource is an Item, or belongs to one, whose Group is archived: the Item stays
    /// readable and read-only.
    /// </summary>
    public bool ChildInArchivedGroup { get; init; }
}

public sealed record AccessPermissionRule(int Priority, string Effect);
