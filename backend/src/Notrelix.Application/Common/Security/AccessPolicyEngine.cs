using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Common.Security;

public sealed class AccessPolicyEngine : IAccessPolicyEvaluator
{
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

        if (descriptor.Scope is ApplicationScopeKind.Account or ApplicationScopeKind.Workspace or ApplicationScopeKind.Resource
            && !facts.AccountOperational)
        {
            return AccessDecision.Deny(
                AccessDecisionKind.Forbidden,
                "This account is not operational.");
        }

        if (descriptor.Scope is ApplicationScopeKind.Account or ApplicationScopeKind.Workspace or ApplicationScopeKind.Resource
            && !facts.UserOperational)
        {
            return AccessDecision.Deny(
                AccessDecisionKind.Forbidden,
                "This user is not operational.");
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
            return AccessDecision.Allow();
        }

        if (isAccount)
        {
            if (permission.Action == PermissionAction.ViewWorkspace
                || permission.Action == PermissionAction.CreateWorkspace
                && string.Equals(role, "Admin", StringComparison.Ordinal))
            {
                return AccessDecision.Allow();
            }

            return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
        }

        if (permission.Action == PermissionAction.DeleteWorkspace)
        {
            return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
        }

        // WorkspaceAdmin administrative baseline (WG-ROLE-DEC-001): an Admin performs
        // workspace-scope administration without a custom rule. Board management stays
        // resource-owned (Phase 8), Owner-only actions stay Owner-only, and the grant is
        // scoped to Workspace requests so it never leaks into Resource/board scope.
        if (descriptor.Scope == ApplicationScopeKind.Workspace
            && string.Equals(role, "Admin", StringComparison.Ordinal)
            && IsWorkspaceAdministrativeAction(permission.Action))
        {
            return AccessDecision.Allow();
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

            // Built-in role baseline (WG-ROLE-DEC-001): a plain Workspace member
            // holds collaboration-class authority on a Workspace-visible board.
            // Board-management authority is resource-owned and requires an explicit
            // Board-level grant (board owner/admin role or an explicit resource
            // permission), never Workspace visibility alone.
            var hasBoardAuthority = IsBoardManagementRole(facts.ResourceMemberRole)
                || facts.HasExplicitResourcePermission;

            if (IsBoardManagementAction(permission.Action) && !hasBoardAuthority)
            {
                return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
            }

            if ((permission.Action is PermissionAction.UpdateItem or PermissionAction.MoveItem or PermissionAction.AssignItem)
                && string.Equals(facts.ResourceMemberRole, "Observer", StringComparison.Ordinal)
                && !facts.HasExplicitResourcePermission)
            {
                return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
            }

            return AccessDecision.Allow();
        }

        return permission.Action is PermissionAction.ViewWorkspace or PermissionAction.ViewBoard or PermissionAction.ViewMembers
            ? AccessDecision.Allow()
            : AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
    }

    private static bool IsBoardManagementRole(string? resourceMemberRole) =>
        string.Equals(resourceMemberRole, "Owner", StringComparison.Ordinal)
        || string.Equals(resourceMemberRole, "Admin", StringComparison.Ordinal);

    private static bool IsBoardManagementAction(PermissionAction action) =>
        action is PermissionAction.ManageBoard
            or PermissionAction.ManageBoardPermission
            or PermissionAction.CreateField
            or PermissionAction.UpdateField
            or PermissionAction.DeleteField
            or PermissionAction.ShareBoardView;

    private static bool IsWorkspaceAdministrativeAction(PermissionAction action) =>
        action is PermissionAction.ManageWorkspace
            or PermissionAction.InviteMember
            or PermissionAction.ChangeMemberRole
            or PermissionAction.RemoveMember
            or PermissionAction.ManageWorkspaceSettings
            or PermissionAction.CreateBoard;

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
