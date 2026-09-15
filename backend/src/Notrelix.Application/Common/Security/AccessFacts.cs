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
    int? TargetPermissionRank);

public sealed record AccessPermissionRule(int Priority, string Effect);
