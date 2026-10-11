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
}

public sealed record AccessPermissionRule(int Priority, string Effect);
