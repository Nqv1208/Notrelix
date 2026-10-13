using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Governance.Public.Access;

namespace Notrelix.Application.Features.Governance.Authorization;

public sealed class AccessPolicyEngine : IAccessPolicyEvaluator
{
    // Technical rank ordering for Governance permission levels: None=0,
    // Viewer=1, Commenter=2, Editor=3, Manager=4, Owner=5. The pipeline seam
    // compares ranks only; the vocabulary itself stays Governance-owned.
    private const int ViewerRank = 1;
    private const int CommenterRank = 2;
    private const int EditorRank = 3;
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
            var permission = EvaluatePermission(descriptor, facts, request, context.UserId);
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

        if (descriptor.Access.RequiresSubscription && !facts.SubscriptionRequirementSatisfied)
        {
            // Billing owns the active-subscription + tier decision; the facts
            // provider composed the neutral result. Governance reads the
            // requested minimum tier here only to render the denial message —
            // it never interprets or orders subscription tiers.
            var minimumTier = request is IRequireSubscription subscription ? subscription.MinimumTier : null;
            var message = string.IsNullOrEmpty(minimumTier)
                ? "This feature requires an active subscription."
                : $"This feature requires at least the '{minimumTier}' subscription tier.";
            return AccessDecision.Deny(AccessDecisionKind.Forbidden, message);
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
        object request,
        Guid? actorId)
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

        // A Board child whose Board cannot be resolved is NotFound in any scope;
        // it never falls back to the resource-kind fallthrough (GOV-033).
        if (facts.IsBoardChild && facts.ParentBoardId is null)
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
            return GrantAwareAllow(request, facts, role, actorId);
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

        // GOV-028: Spaces are managed by workspace Owners/Admins anywhere, and by
        // an effective Space Admin within the Space's subtree. Creating a Board or
        // Page in a Space needs Editor level on that Space (GOV-027).
        if (permission.Resource?.Kind.Value == "workspaces.space")
        {
            return EvaluateSpace(permission, facts, role);
        }

        // GOV-028: root Space creation and Team composition are workspace Owner/Admin
        // capabilities by default (Owners passed the fast path above). Explicit
        // permission rules were evaluated first.
        if (permission.Resource?.Kind.Value == "workspaces.workspace"
            && permission.Action is PermissionAction.ManageSpaces or PermissionAction.ManageTeams)
        {
            return string.Equals(role, "Admin", StringComparison.Ordinal)
                ? AccessDecision.Allow()
                : AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
        }

        if (permission.Action == PermissionAction.DeleteWorkspace)
        {
            return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
        }

        // A Board child resource is decided on its parent Board (GOV-033): the
        // facts are the Board's, so the Board policy applies to the child's action.
        if (permission.Resource?.Kind.Value == "work-management.board" || facts.ParentBoardId is not null)
        {
            if (!facts.ResourceExists)
            {
                return AccessDecision.Deny(AccessDecisionKind.NotFound, "Resource not found.");
            }

            var access = EvaluatePlacedResourceAccess(permission, facts, role);
            if (access.Denial is { } denied)
            {
                return denied;
            }

            // A Board child accepts only the Board-child action vocabulary; an
            // unrelated action (a workspace-level share, Space management, …)
            // is refused rather than gated by the Board level.
            if (facts.ParentBoardId is not null && !IsBoardChildAction(permission.Action))
            {
                return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
            }

            if (permission.Action == PermissionAction.UpdateItem
                && string.Equals(facts.ResourceMemberRole, "Observer", StringComparison.Ordinal))
            {
                return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
            }

            return ManageAwareAllow(permission, request, facts, role, access.EffectiveRank, actorId);
        }

        if (permission.Resource?.Kind.Value == "documents.page")
        {
            if (!facts.ResourceExists)
            {
                return AccessDecision.Deny(AccessDecisionKind.NotFound, "Resource not found.");
            }

            var access = EvaluatePlacedResourceAccess(permission, facts, role);
            if (access.Denial is { } denied)
            {
                return denied;
            }

            // M2G extension (reviewer-approved): page lifecycle mutation
            // authority is distinct from page visibility — a workspace-visible
            // page only proves the actor can see the resource, not archive it.
            // ArchivePage requires an active page ResourcePermission of at
            // least Manager (owners pass the earlier fast-path, explicit
            // permission rules were evaluated above). It carries its own
            // Governance vocabulary: neither ManageBoard nor
            // ManagePagePermission (ACL management).
            if (permission.Action == PermissionAction.ArchivePage
                && access.EffectiveRank < ManagerRank)
            {
                return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
            }

            return ManageAwareAllow(permission, request, facts, role, access.EffectiveRank, actorId);
        }

        // M8 intended policy: connecting/revoking external provider
        // integrations is a dedicated workspace capability — not workspace
        // settings, and not any Board/Page vocabulary. Owner and Admin hold
        // it by default; ordinary members and guests do not. The capability
        // applies to both addressing surfaces: the workspace (Connect) and
        // the integration-owned resource (Disconnect). Applicable explicit
        // permission rules still evaluate first above.
        if (permission.Action == PermissionAction.ManageIntegrations
            && permission.Resource?.Kind.Value
                is "workspaces.workspace" or "integrations.calendar-integration")
        {
            var privileged = string.Equals(role, "Owner", StringComparison.Ordinal)
                || string.Equals(role, "Admin", StringComparison.Ordinal);
            return privileged
                ? AccessDecision.Allow()
                : AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
        }

        // M2G intended policy: page creation is a workspace usage right —
        // the DC-FLOW-01 actor is the authenticated workspace member, not
        // workspace management. Guests are excluded; explicit permission
        // rules still deny-first above. Other workspace-scoped actions keep
        // the fall-through contract.
        if (permission.Resource?.Kind.Value == "workspaces.workspace"
            && permission.Action == PermissionAction.CreatePage)
        {
            var guest = string.Equals(role, "Guest", StringComparison.Ordinal);
            return guest
                ? AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.")
                : AccessDecision.Allow();
        }

        // M2G intended policy: commenting on a board item is a usage right
        // decided by the target resource kind, not a mechanical copy of the
        // ManageBoard authority mapping. Board items carry no per-item
        // audience fact, so guests need explicit access evidence on the item
        // itself. Other board-item actions keep the fall-through contract.
        if (permission.Resource?.Kind.Value == "work-management.board-item"
            && permission.Action == PermissionAction.CreateComment)
        {
            if (!facts.ResourceExists)
            {
                return AccessDecision.Deny(AccessDecisionKind.NotFound, "Resource not found.");
            }

            var guest = string.Equals(role, "Guest", StringComparison.Ordinal);
            if (guest && !facts.HasExplicitResourcePermission)
            {
                return AccessDecision.Deny(AccessDecisionKind.NotFound, "Resource not found.");
            }

            return GrantAwareAllow(request, facts, role, actorId);
        }

        return permission.Action is PermissionAction.ViewWorkspace or PermissionAction.ViewBoard or PermissionAction.ViewMembers
            ? AccessDecision.Allow()
            : AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
    }

    /// <summary>
    /// The principal's effective level on a Board and its source, over the same
    /// facts as the single read (<c>ViewBoard</c>), in GOV-025 order: a missing
    /// resource or no Workspace membership → none; workspace Owner → Owner;
    /// a first-priority Deny rule → none. Otherwise the level the engine
    /// enforces for Board actions (<see cref="ResolveResourceLevel"/>: the
    /// highest level among every source, a Guest capped at Editor). An Allow
    /// rule with no other source gives Viewer. The result is none exactly when
    /// the single read is denied; it describes access for clients and never
    /// replaces the per-action decision.
    /// </summary>
    public static EffectiveResourceAccess EvaluateEffectiveBoardAccess(AccessFacts facts)
    {
        var role = facts.WorkspaceMemberRole;
        if (role is null || !facts.ResourceExists)
        {
            return EffectiveResourceAccess.None;
        }

        if (string.Equals(role, "Owner", StringComparison.Ordinal))
        {
            return new EffectiveResourceAccess(OwnerRank, EffectiveAccessBasis.WorkspaceOwner);
        }

        var firstPriority = facts.PermissionRules.Count == 0
            ? []
            : facts.PermissionRules
                .Where(rule => rule.Priority == facts.PermissionRules.Min(candidate => candidate.Priority))
                .ToArray();
        if (firstPriority.Any(rule => string.Equals(rule.Effect, "Deny", StringComparison.OrdinalIgnoreCase)))
        {
            return EffectiveResourceAccess.None;
        }

        var best = ResolveResourceLevel(facts, role);

        if (best.Basis == EffectiveAccessBasis.None
            && firstPriority.Any(rule => string.Equals(rule.Effect, "Allow", StringComparison.OrdinalIgnoreCase)))
        {
            return new EffectiveResourceAccess(ViewerRank, EffectiveAccessBasis.PermissionRule);
        }

        return best;
    }

    private static EffectiveResourceAccess Higher(EffectiveResourceAccess current, EffectiveResourceAccess candidate) =>
        candidate.Rank > current.Rank ? candidate : current;

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
        string? role,
        int effectiveRank,
        Guid? actorId)
    {
        var isManagementAction = permission.Action
            is PermissionAction.ManageBoardPermission or PermissionAction.ManagePagePermission;
        if (isManagementAction
            && !string.Equals(role, "Owner", StringComparison.Ordinal)
            && effectiveRank < ManagerRank)
        {
            return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
        }

        return GrantAwareAllow(request, facts, role, effectiveRank, actorId);
    }

    private static AccessDecision GrantAwareAllow(object request, AccessFacts facts, string? role, Guid? actorId) =>
        GrantAwareAllow(request, facts, role, facts.ActiveResourcePermissionRank ?? 0, actorId);

    /// <summary>
    /// Grant ceiling: the caller's authority (their effective level, Owner for a
    /// workspace Owner) must reach the requested level and any higher level the
    /// target already holds. A caller below Manager may not raise their own board
    /// role (assigning a board role is a grant of its level, see
    /// <see cref="BoardRoleLevels"/>): that would lift limits such as Observer's.
    /// </summary>
    private static AccessDecision GrantAwareAllow(object request, AccessFacts facts, string? role, int effectiveRank, Guid? actorId)
    {
        if (request is IRequireGrantPermission grant)
        {
            var authority = EffectiveGrantAuthority(effectiveRank, role);
            if (authority < ManagerRank
                && request is IRequirePermissionTarget { TargetSubjectType: "User", TargetSubjectId: { } target }
                && actorId is { } actor && target == actor
                && grant.RequestedPermissionRank > BoardRoleLevels.RankOf(facts.ResourceMemberRole))
            {
                return AccessDecision.Deny(AccessDecisionKind.Forbidden, "You cannot raise your own access level.");
            }

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

            var authority = EffectiveGrantAuthority(effectiveRank, role);
            return facts.TargetPermissionRank > 0 && authority >= facts.TargetPermissionRank
                ? AccessDecision.Allow()
                : AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to revoke this permission level.");
        }

        return AccessDecision.Allow();
    }

    private static int EffectiveGrantAuthority(int effectiveRank, string? role) =>
        string.Equals(role, "Owner", StringComparison.Ordinal)
            ? OwnerRank
            : effectiveRank;

    private readonly record struct PlacedResourceAccess(AccessDecision? Denial, int EffectiveRank);

    /// <summary>
    /// GOV-025 steps 4–6 for a Board or Page (existence, Owner and explicit
    /// permission rules were decided earlier). Visibility is unchanged: a
    /// Private resource needs board membership or an explicit grant and Space
    /// roles never reach it; a Workspace-visible resource placed in a Space is
    /// reached by a Space level, an unplaced one by the Workspace default
    /// (Guests need explicit access); no source → NotFound. The resource level
    /// is the highest level among every source (<see cref="ResolveResourceLevel"/>),
    /// a Workspace Guest is capped at Editor (GOV-030), and the action threshold
    /// of GOV-027 applies to that level whatever the source → Forbidden below it.
    /// The same level is the authority for ACL management and grant ceilings.
    /// </summary>
    private static PlacedResourceAccess EvaluatePlacedResourceAccess(
        IRequirePermission permission,
        AccessFacts facts,
        string? role)
    {
        var level = ResolveResourceLevel(facts, role);
        if (level.Basis == EffectiveAccessBasis.None)
        {
            return new PlacedResourceAccess(AccessDecision.Deny(AccessDecisionKind.NotFound, "Resource not found."), 0);
        }

        return level.Rank < RequiredRank(permission.Action)
            ? new PlacedResourceAccess(
                AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action."),
                level.Rank)
            : new PlacedResourceAccess(null, level.Rank);
    }

    /// <summary>
    /// The level every access source gives on a Board or Page, and the highest
    /// of them (ties keep the earlier source: explicit grant, board membership,
    /// Space / workspace Admin, Workspace default):
    /// <list type="bullet">
    /// <item>explicit resource permission (User or active Team): its level (a
    ///   level-less entry still reads, i.e. Viewer);</item>
    /// <item>board membership: <see cref="BoardRoleRank"/>;</item>
    /// <item>Workspace-visible resource placed in a Space: the Space level
    ///   (<see cref="SpaceRank"/>, workspace Admin → Manager);</item>
    /// <item>unplaced Workspace-visible resource: Editor for non-Guest members
    ///   (every Board/Page action but permission management).</item>
    /// </list>
    /// A Workspace Guest's level is capped at Editor whatever the source
    /// (GOV-030). No source → <see cref="EffectiveResourceAccess.None"/>.
    /// </summary>
    private static EffectiveResourceAccess ResolveResourceLevel(AccessFacts facts, string? role)
    {
        var guest = string.Equals(role, "Guest", StringComparison.Ordinal);
        var best = EffectiveResourceAccess.None;

        // The explicit rank only exists for an active grant; either fact marks the source.
        if (facts.HasExplicitResourcePermission || facts.ActiveResourcePermissionRank > 0)
        {
            best = Higher(best, new EffectiveResourceAccess(
                Math.Max(facts.ActiveResourcePermissionRank ?? 0, ViewerRank), EffectiveAccessBasis.ResourcePermission));
        }

        if (facts.ResourceMemberRole is { } boardRole)
        {
            best = Higher(best, new EffectiveResourceAccess(BoardRoleRank(boardRole), EffectiveAccessBasis.BoardMembership));
        }

        if (string.Equals(facts.ResourceAudience, "Workspace", StringComparison.Ordinal))
        {
            if (facts.ResourceSpaceId is not null)
            {
                var spaceRank = SpaceRank(facts, role);
                if (spaceRank > 0)
                {
                    var basis = string.Equals(role, "Admin", StringComparison.Ordinal)
                        ? EffectiveAccessBasis.WorkspaceAdmin
                        : EffectiveAccessBasis.Space;
                    best = Higher(best, new EffectiveResourceAccess(spaceRank, basis));
                }
            }
            else if (!guest)
            {
                best = Higher(best, new EffectiveResourceAccess(EditorRank, EffectiveAccessBasis.WorkspaceDefault));
            }
        }

        return guest && best.Rank > EditorRank ? best with { Rank = EditorRank } : best;
    }

    /// <summary>
    /// Board membership roles map to levels (the single mapping table):
    /// Owner, Admin → Manager; Member → Editor; Observer → Viewer (reads only);
    /// Guest → Viewer (no documented edit semantics; least privilege). An
    /// unknown role reads only.
    /// </summary>
    private static int BoardRoleRank(string boardRole) => BoardRoleLevels.RankOf(boardRole);

    /// <summary>The actions a Board child resource accepts (GOV-033).</summary>
    private static bool IsBoardChildAction(PermissionAction action) => action
        is PermissionAction.ViewBoard or PermissionAction.ViewMembers or PermissionAction.CreateComment
        or PermissionAction.CreateItem or PermissionAction.UpdateItem or PermissionAction.MoveItem or PermissionAction.AssignItem
        or PermissionAction.CreateField or PermissionAction.UpdateField or PermissionAction.DeleteField
        or PermissionAction.CreateBoardView or PermissionAction.UpdateBoardView
        or PermissionAction.ManageBoard or PermissionAction.ManageBoardPermission;

    /// <summary>
    /// GOV-028 for a Space resource: invisible (or missing) → NotFound; otherwise
    /// the effective Space level must meet the action threshold (ManageSpaces →
    /// Manager, i.e. Space Admin; creating a Board/Page there → Editor).
    /// </summary>
    private static AccessDecision EvaluateSpace(IRequirePermission permission, AccessFacts facts, string? role)
    {
        if (!facts.SpaceExists)
        {
            return AccessDecision.Deny(AccessDecisionKind.NotFound, "Resource not found.");
        }

        var rank = SpaceRank(facts, role);
        if (rank == 0)
        {
            return AccessDecision.Deny(AccessDecisionKind.NotFound, "Resource not found.");
        }

        return rank >= RequiredRank(permission.Action)
            ? AccessDecision.Allow()
            : AccessDecision.Deny(AccessDecisionKind.Forbidden, "You do not have permission to perform this action.");
    }

    /// <summary>
    /// Effective level on the target Space (GOV-025/GOV-027): workspace Owner/Admin
    /// → Manager on every Space; Space Admin → Manager, Member → Editor, Viewer →
    /// Viewer (from User or active Team entries, inheritance as resolved by the
    /// facts); an open Space (no Private Space from the root) gives workspace
    /// Members Editor. Guests get nothing implicitly. 0 = no access.
    /// </summary>
    private static int SpaceRank(AccessFacts facts, string? role)
    {
        if (!facts.SpaceExists)
        {
            return 0;
        }

        if (string.Equals(role, "Owner", StringComparison.Ordinal) || string.Equals(role, "Admin", StringComparison.Ordinal))
        {
            return ManagerRank;
        }

        var fromSpaceRole = facts.SpaceRole switch
        {
            "Admin" => ManagerRank,
            "Member" => EditorRank,
            "Viewer" => ViewerRank,
            _ => 0,
        };
        var fromOpenSpace = facts.SpaceIsOpen && string.Equals(role, "Member", StringComparison.Ordinal) ? EditorRank : 0;
        return Math.Max(fromSpaceRole, fromOpenSpace);
    }

    /// <summary>GOV-027 action thresholds for access that comes from a Space level.</summary>
    private static int RequiredRank(PermissionAction action) => action switch
    {
        PermissionAction.ViewBoard or PermissionAction.ViewPage
            or PermissionAction.ViewWorkspace or PermissionAction.ViewMembers => ViewerRank,
        PermissionAction.CreateComment => CommenterRank,
        PermissionAction.ManageBoardPermission or PermissionAction.ManagePagePermission
            or PermissionAction.ArchivePage or PermissionAction.ManageSpaces => ManagerRank,
        _ => EditorRank,
    };
}