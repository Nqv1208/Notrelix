using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Common.Security;

public sealed class AccessPolicyEngine : IAccessPolicyEvaluator
{
    // Technical rank ordering for Governance permission levels: None=0,
    // Viewer=1, Commenter=2, Editor=3, Manager=4, Owner=5. The pipeline seam
    // compares ranks only; the vocabulary itself stays Governance-owned.
    private const int ManagerRank = 4;
    private const int OwnerRank = 5;

    public AccessDecision Evaluate(
        RequestDescriptor descriptor,
        ExecutionContextSnapshot context,
        AccessFacts facts,
        object request)
    {
        if (descriptor.Principal == ApplicationPrincipalKind.Anonymous
            || descriptor.Principal == ApplicationPrincipalKind.System)
        {
            return AccessDecision.Allow();
        }

        if (!context.UserId.HasValue || context.UserId == Guid.Empty)
        {
            return AccessDecision.Deny(AccessDecisionKind.Unauthorized, "Authentication required.");
        }

        if (descriptor.Scope is ApplicationScopeKind.Account or ApplicationScopeKind.Workspace or ApplicationScopeKind.Resource
            && (!context.AccountId.HasValue || context.AccountId == Guid.Empty))
        {
            return AccessDecision.Deny(
                AccessDecisionKind.SecurityMisconfiguration,
                $"{descriptor.RequestType.Name} requires account context.");
        }

        if (descriptor.Scope is ApplicationScopeKind.Workspace or ApplicationScopeKind.Resource
            && (!context.WorkspaceId.HasValue || context.WorkspaceId == Guid.Empty))
        {
            return AccessDecision.Deny(
                AccessDecisionKind.SecurityMisconfiguration,
                $"{descriptor.RequestType.Name} requires workspace context.");
        }

        if (descriptor.Access.RequiresPermission)
        {
            var permission = EvaluatePermission(descriptor, facts, request);
            if (permission.Kind != AccessDecisionKind.Allowed)
            {
                return permission;
            }
        }

        if (descriptor.Access.RequiresVerifiedEmail)
        {
            if (!facts.UserExists)
            {
                return AccessDecision.Deny(AccessDecisionKind.Unauthorized, "Authentication required.");
            }

            if (!facts.EmailVerified)
            {
                return AccessDecision.Deny(
                    AccessDecisionKind.Forbidden,
                    "Email must be confirmed before using this feature.");
            }
        }

        if (descriptor.Access.RequiresSubscription && request is IRequireSubscription subscription)
        {
            if (!facts.HasActiveSubscription
                || !MeetsMinimumTier(facts.SubscriptionTier, subscription.MinimumTier))
            {
                var message = string.IsNullOrEmpty(subscription.MinimumTier)
                    ? "This feature requires an active subscription."
                    : $"This feature requires at least the '{subscription.MinimumTier}' subscription tier.";
                return AccessDecision.Deny(AccessDecisionKind.Forbidden, message);
            }
        }

        if (descriptor.Access.RequiresFeature && !facts.FeatureEnabled)
        {
            var code = request is IRequireFeature feature ? feature.FeatureCode : "unknown";
            return AccessDecision.Deny(
                AccessDecisionKind.Forbidden,
                $"Feature '{code}' is not enabled for this account or usage limit reached.");
        }

        return AccessDecision.Allow();
    }

    private static AccessDecision EvaluatePermission(
        RequestDescriptor descriptor,
        AccessFacts facts,
        object request)
    {
        if (request is not IRequirePermission permission)
        {
            return AccessDecision.Deny(
                AccessDecisionKind.SecurityMisconfiguration,
                $"{descriptor.RequestType.Name} has no permission contract.");
        }

        var isAccount = descriptor.Scope == ApplicationScopeKind.Account;
        var role = isAccount ? facts.AccountMemberRole : facts.WorkspaceMemberRole;
        if (role is null)
        {
            return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
        }

        // Resource lifecycle comes before every authority fast-path: a
        // missing/archived/deleted resource is NotFound even for owners.
        if (descriptor.Scope == ApplicationScopeKind.Resource
            && !facts.ResourceExists)
        {
            return AccessDecision.Deny(AccessDecisionKind.NotFound, "Resource not found.");
        }

        if (string.Equals(role, "Owner", StringComparison.Ordinal))
        {
            return AccessDecision.Allow();
        }

        var firstPriority = facts.PermissionRules.Count == 0
            ? []
            : facts.PermissionRules
                .Where(rule => rule.Priority == facts.PermissionRules.Min(candidate => candidate.Priority))
                .ToArray();
        if (firstPriority.Any(rule => string.Equals(rule.Effect, "Deny", StringComparison.OrdinalIgnoreCase)))
        {
            return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
        }

        if (firstPriority.Any(rule => string.Equals(rule.Effect, "Allow", StringComparison.OrdinalIgnoreCase)))
        {
            return GrantAwareAllow(request, facts, role);
        }

        if (isAccount)
        {
            var isAdmin = string.Equals(role, "Admin", StringComparison.Ordinal);
            if (permission.Action == PermissionAction.ViewWorkspace
                || (isAdmin && permission.Action == PermissionAction.CreateWorkspace)
                || (isAdmin && permission.Action == PermissionAction.ManageAccount))
            {
                return AccessDecision.Allow();
            }

            return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
        }

        if (permission.Action == PermissionAction.DeleteWorkspace)
        {
            return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
        }

        if (permission.Resource?.Kind.Value == "work-management.board")
        {
            if (!facts.ResourceExists)
            {
                return AccessDecision.Deny(AccessDecisionKind.NotFound, "Resource not found.");
            }

            var restricted = !string.Equals(facts.ResourceAudience, "Workspace", StringComparison.Ordinal);
            var guest = string.Equals(role, "Guest", StringComparison.Ordinal);
            if ((restricted || guest)
                && facts.ResourceMemberRole is null
                && !facts.HasExplicitResourcePermission)
            {
                return AccessDecision.Deny(AccessDecisionKind.NotFound, "Resource not found.");
            }

            if (permission.Action == PermissionAction.UpdateItem
                && string.Equals(facts.ResourceMemberRole, "Observer", StringComparison.Ordinal))
            {
                return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
            }

            return ManageAwareAllow(permission, request, facts, role);
        }

        if (permission.Resource?.Kind.Value == "documents.page")
        {
            if (!facts.ResourceExists)
            {
                return AccessDecision.Deny(AccessDecisionKind.NotFound, "Resource not found.");
            }

            var restricted = !string.Equals(facts.ResourceAudience, "Workspace", StringComparison.Ordinal);
            var guest = string.Equals(role, "Guest", StringComparison.Ordinal);
            if ((restricted || guest)
                && facts.ResourceMemberRole is null
                && !facts.HasExplicitResourcePermission)
            {
                return AccessDecision.Deny(AccessDecisionKind.NotFound, "Resource not found.");
            }

            return ManageAwareAllow(permission, request, facts, role);
        }

        return permission.Action is PermissionAction.ViewWorkspace or PermissionAction.ViewBoard or PermissionAction.ViewMembers
            ? AccessDecision.Allow()
            : AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
    }

    /// <summary>
    /// Managing a resource's ACL (viewing or mutating governance.resource_permissions)
    /// requires explicit management authority: workspace owners, or an active
    /// resource permission of at least Manager. Ordinary workspace visibility
    /// never grants ACL management. Grant/revoke requests still pass through the
    /// grant ceiling afterwards.
    /// </summary>
    private static AccessDecision ManageAwareAllow(
        IRequirePermission permission,
        object request,
        AccessFacts facts,
        string? role)
    {
        var isManagementAction = permission.Action
            is PermissionAction.ManageBoardPermission or PermissionAction.ManagePagePermission;
        if (isManagementAction
            && !string.Equals(role, "Owner", StringComparison.Ordinal)
            && (facts.ActiveResourcePermissionRank ?? 0) < ManagerRank)
        {
            return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
        }

        return GrantAwareAllow(request, facts, role);
    }

    private static AccessDecision GrantAwareAllow(object request, AccessFacts facts, string? role)
    {
        if (request is IRequireGrantPermission grant)
        {
            var authority = EffectiveGrantAuthority(facts, role);
            var ceiling = grant.RequestedPermissionRank;
            if (facts.TargetPermissionRank is { } existingRank && existingRank > ceiling)
            {
                ceiling = existingRank;
            }

            return ceiling > 0 && authority >= ceiling
                ? AccessDecision.Allow()
                : AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to grant this permission level.");
        }

        if (request is IRequireRevokePermission)
        {
            if (facts.TargetPermissionRank is null)
            {
                return AccessDecision.Allow();
            }

            var authority = EffectiveGrantAuthority(facts, role);
            return facts.TargetPermissionRank > 0 && authority >= facts.TargetPermissionRank
                ? AccessDecision.Allow()
                : AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to revoke this permission level.");
        }

        return AccessDecision.Allow();
    }

    private static int EffectiveGrantAuthority(AccessFacts facts, string? role) =>
        string.Equals(role, "Owner", StringComparison.Ordinal)
            ? OwnerRank
            : facts.ActiveResourcePermissionRank ?? 0;

    private static bool MeetsMinimumTier(string? actualTier, string? minimumTier)
    {
        if (string.IsNullOrEmpty(minimumTier))
        {
            return true;
        }

        var tiers = new[] { "Free", "Starter", "Pro", "Business", "Enterprise" };
        return Array.IndexOf(tiers, actualTier) >= Array.IndexOf(tiers, minimumTier);
    }
}