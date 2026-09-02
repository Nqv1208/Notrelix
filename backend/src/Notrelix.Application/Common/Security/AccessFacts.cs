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
    bool HasActiveSubscription,
    string? SubscriptionTier,
    bool FeatureEnabled,
    bool AccountOperational,
    bool UserOperational);

public sealed record AccessPermissionRule(int Priority, string Effect);
