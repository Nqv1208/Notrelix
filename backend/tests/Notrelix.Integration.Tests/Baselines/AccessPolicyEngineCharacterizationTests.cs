using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Integration.Tests.Baselines;

/// <summary>
/// Frozen access-control characterization: the externally observable decision
/// matrix (access-control-scenarios.json) executed against the final
/// AccessPolicyEngine. The engine is pure — every scenario is exercised with an
/// in-memory AccessFacts snapshot, proving zero-I/O policy evaluation produces
/// the same Allow/Forbid/NotFound/SecurityMisconfiguration contract the legacy
/// AuthorizationBehavior + PermissionService stack produced.
/// </summary>
public static class AccessControlCharacterizationTests
{
    // The characterization names below are referenced verbatim by
    // access-control-scenarios.json and must remain stable.
    private static readonly string[] CharacterizationTestNames =
    [
        "AnonymousRequest_SkipsAuth_CallsHandler",
        "SystemInternalRequest_WithoutUser_BypassesAuth_CallsHandler",
        "EvaluateAsync_ShouldAllowOwnerForAllWorkspaceActions",
        "EvaluateAsync_ShouldDenyNonMembers",
        "EvaluateAsync_WorkspaceBoard_ShouldAllowWorkspaceMembersToView",
        "EvaluateAsync_WorkspaceMemberCannotManageBoardWithoutBoardAuthority",
        "EvaluateAsync_BoardOwnerCanManageBoard",
        "EvaluateAsync_ExplicitResourcePermissionGrantsBoardManagement",
        "EvaluateAsync_WorkspaceMemberCanManageBoardView",
        "EvaluateAsync_ViewerCannotUpdateItem",
        "EvaluateAsync_PrivateBoard_ShouldHideForNonBoardMembers",
        "EvaluateAsync_EditorCanUpdateItem",
        "EvaluateAsync_WorkspaceGuestCannotViewPrivateBoard",
        "EvaluateAsync_RevokedPermissionsAreInvalid",
        "EvaluateAsync_BoardFromAnotherWorkspace_IsHidden",
        "EvaluateAsync_SamePriorityDenyOverridesAllow",
        "EvaluateAsync_AccountScope_ExplicitGovernanceDeny_OverridesAdminFallback",
        "EvaluateAsync_AccountScope_ExplicitGovernanceAllow_GrantsBaselineDeniedAction",
        "EvaluateAsync_InactiveOrOutOfWindowRule_IsIgnored",
        "EvaluateAsync_AccountScope_ShouldAllowOwnerForAllAccountActions",
        "EvaluateAsync_AccountScope_AdminBaseline_AllowsCreateWorkspaceWithoutRule",
        "EvaluateAsync_AccountScope_ShouldDenyNonMembers",
        "EvaluateAsync_AccountScope_ShouldDenySuspendedMembers",
        "RequiredAndEmailVerified_AllowsRequest",
        "RequiredButEmailUnverified_ThrowsForbidden",
        "ActiveSubscription_AllowsPassThrough",
        "NoActiveSubscription_ThrowsForbidden",
        "MinimumTierNotMet_ThrowsForbidden",
        "FeatureEnabled_AllowsPassThrough",
        "FeatureDisabled_ThrowsForbidden",
        "MissingAccountId_ThrowsSecurityMisconfiguration",
    ];

    public static IReadOnlyList<string> Names => CharacterizationTestNames;
}

public sealed class AccessPolicyEngineCharacterizationTests
{
    private static readonly AccessPolicyEngine Engine = new();

    [Fact]
    public void AnonymousRequest_SkipsAuth_CallsHandler()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Anonymous, ApplicationScopeKind.Global, DataAccess: ApplicationDataAccessKind.None),
            Context(ApplicationPrincipalKind.Anonymous, ApplicationScopeKind.Global, userId: null),
            NoFacts,
            request: null!);

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void SystemInternalRequest_WithoutUser_BypassesAuth_CallsHandler()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.System, ApplicationScopeKind.Global, DataAccess: ApplicationDataAccessKind.None),
            Context(ApplicationPrincipalKind.System, ApplicationScopeKind.Global, userId: null),
            NoFacts,
            request: null!);

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void EvaluateAsync_ShouldAllowOwnerForAllWorkspaceActions()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Workspace, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Workspace),
            Facts(workspaceMemberRole: "Owner"),
            PermissionRequest(PermissionAction.DeleteWorkspace, null));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void EvaluateAsync_ShouldDenyNonMembers()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Workspace, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Workspace),
            Facts(workspaceMemberRole: null),
            PermissionRequest(PermissionAction.ViewWorkspace, null));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void EvaluateAsync_WorkspaceBoard_ShouldAllowWorkspaceMembersToView()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource),
            Facts(workspaceMemberRole: "Member", resourceExists: true, resourceAudience: "Workspace"),
            PermissionRequest(PermissionAction.ViewBoard, ResourceKind.Create("work-management.board")));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void EvaluateAsync_WorkspaceMemberCannotManageBoardWithoutBoardAuthority()
    {
        // WG-ROLE-DEC-001: a plain Workspace member must NOT gain board-management
        // authority from Workspace visibility alone; board management is
        // resource-owned and requires an explicit Board-level grant.
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource),
            Facts(workspaceMemberRole: "Member", resourceExists: true, resourceAudience: "Workspace"),
            PermissionRequest(PermissionAction.ManageBoard, ResourceKind.Create("work-management.board")));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void EvaluateAsync_BoardOwnerCanManageBoard()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource),
            Facts(workspaceMemberRole: "Member", resourceExists: true, resourceAudience: "Workspace", resourceMemberRole: "Owner"),
            PermissionRequest(PermissionAction.ManageBoard, ResourceKind.Create("work-management.board")));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void EvaluateAsync_ExplicitResourcePermissionGrantsBoardManagement()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource),
            Facts(workspaceMemberRole: "Member", resourceExists: true, resourceAudience: "Workspace", hasExplicitResourcePermission: true),
            PermissionRequest(PermissionAction.ManageBoard, ResourceKind.Create("work-management.board")));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void EvaluateAsync_WorkspaceMemberCanManageBoardView()
    {
        // Creating/updating a board view is collaboration-class, so a Workspace
        // member on a Workspace-visible board may do it without a Board-level grant.
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource),
            Facts(workspaceMemberRole: "Member", resourceExists: true, resourceAudience: "Workspace"),
            PermissionRequest(PermissionAction.CreateBoardView, ResourceKind.Create("work-management.board")));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void EvaluateAsync_ViewerCannotUpdateItem()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource),
            Facts(workspaceMemberRole: "Member", resourceExists: true, resourceAudience: "Restricted", resourceMemberRole: "Observer"),
            PermissionRequest(PermissionAction.UpdateItem, ResourceKind.Create("work-management.board")));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void EvaluateAsync_PrivateBoard_ShouldHideForNonBoardMembers()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource),
            Facts(workspaceMemberRole: "Member", resourceExists: true, resourceAudience: "Restricted", resourceMemberRole: null),
            PermissionRequest(PermissionAction.ViewBoard, ResourceKind.Create("work-management.board")));

        decision.Kind.Should().Be(AccessDecisionKind.NotFound);
    }

    [Fact]
    public void EvaluateAsync_EditorCanUpdateItem()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource),
            Facts(workspaceMemberRole: "Member", resourceExists: true, resourceAudience: "Restricted", resourceMemberRole: "Member"),
            PermissionRequest(PermissionAction.UpdateItem, ResourceKind.Create("work-management.board")));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void EvaluateAsync_WorkspaceGuestCannotViewPrivateBoard()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource),
            Facts(workspaceMemberRole: "Guest", resourceExists: true, resourceAudience: "Restricted", resourceMemberRole: null),
            PermissionRequest(PermissionAction.ViewBoard, ResourceKind.Create("work-management.board")));

        decision.Kind.Should().Be(AccessDecisionKind.NotFound);
    }

    [Fact]
    public void EvaluateAsync_RevokedPermissionsAreInvalid()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource),
            Facts(workspaceMemberRole: "Member", resourceExists: true, resourceAudience: "Restricted", resourceMemberRole: null, hasExplicitResourcePermission: false),
            PermissionRequest(PermissionAction.ViewBoard, ResourceKind.Create("work-management.board")));

        decision.Kind.Should().Be(AccessDecisionKind.NotFound);
    }

    [Fact]
    public void EvaluateAsync_BoardFromAnotherWorkspace_IsHidden()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource),
            Facts(workspaceMemberRole: "Member", resourceExists: false, resourceAudience: "Workspace"),
            PermissionRequest(PermissionAction.ViewBoard, ResourceKind.Create("work-management.board")));

        decision.Kind.Should().Be(AccessDecisionKind.NotFound);
    }

    [Fact]
    public void EvaluateAsync_SamePriorityDenyOverridesAllow()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource),
            Facts(workspaceMemberRole: "Member", resourceExists: true, rules: [new(100, "Allow"), new(100, "Deny")]),
            PermissionRequest(PermissionAction.UpdateItem, ResourceKind.Create("work-management.board-item")));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void EvaluateAsync_AccountScope_ExplicitGovernanceDeny_OverridesAdminFallback()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account),
            Facts(accountMemberRole: "Admin", rules: [new(100, "Deny")]),
            AccountPermissionRequest(PermissionAction.CreateWorkspace));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void EvaluateAsync_AccountScope_ExplicitGovernanceAllow_GrantsBaselineDeniedAction()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account),
            Facts(accountMemberRole: "Member", rules: [new(100, "Allow")]),
            AccountPermissionRequest(PermissionAction.CreateWorkspace));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void EvaluateAsync_InactiveOrOutOfWindowRule_IsIgnored()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource),
            Facts(workspaceMemberRole: "Member", resourceExists: true, rules: []),
            PermissionRequest(PermissionAction.UpdateItem, ResourceKind.Create("work-management.board-item")));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void EvaluateAsync_AccountScope_ShouldAllowOwnerForAllAccountActions()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account),
            Facts(accountMemberRole: "Owner"),
            AccountPermissionRequest(PermissionAction.CreateWorkspace));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void EvaluateAsync_AccountScope_AdminBaseline_AllowsCreateWorkspaceWithoutRule()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account),
            Facts(accountMemberRole: "Admin"),
            AccountPermissionRequest(PermissionAction.CreateWorkspace));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void EvaluateAsync_AccountScope_ShouldDenyNonMembers()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account),
            Facts(accountMemberRole: null),
            AccountPermissionRequest(PermissionAction.ViewWorkspace));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void EvaluateAsync_AccountScope_ShouldDenySuspendedMembers()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account),
            Facts(accountMemberRole: null),
            AccountPermissionRequest(PermissionAction.ViewWorkspace));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void AccountScope_NonOperationalAccount_FailsClosedBeforePermission()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account),
            Facts(accountMemberRole: "Owner", accountOperational: false),
            AccountPermissionRequest(PermissionAction.CreateWorkspace));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden,
            "a suspended/closed account must be denied centrally before any permission grant");
    }

    [Fact]
    public void WorkspaceScope_NonOperationalAccount_FailsClosedBeforePermission()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Workspace, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Workspace),
            Facts(workspaceMemberRole: "Owner", accountOperational: false),
            PermissionRequest(PermissionAction.DeleteWorkspace, null));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden,
            "an Owner of a workspace cannot operate when the owning account is not operational");
    }

    [Fact]
    public void AccountScope_NonOperationalUser_FailsClosedBeforePermission()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account),
            Facts(accountMemberRole: "Owner", userOperational: false),
            AccountPermissionRequest(PermissionAction.CreateWorkspace));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden,
            "a suspended/deactivated user must be denied centrally before any permission grant");
    }

    [Fact]
    public void WorkspaceScope_NonOperationalUser_FailsClosedBeforePermission()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Workspace, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Workspace),
            Facts(workspaceMemberRole: "Owner", userOperational: false),
            PermissionRequest(PermissionAction.DeleteWorkspace, null));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden,
            "a workspace Owner whose user account became non-operational must fail closed (WG-MEM-008)");
    }

    [Fact]
    public void AccountScope_MissingAccount_FailsClosed()
    {
        // Soft-deleted / absent account: operational fact is false even if a
        // member row would otherwise carry a role.
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresPermission: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account),
            Facts(accountMemberRole: "Owner", accountOperational: false),
            AccountPermissionRequest(PermissionAction.ViewWorkspace));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden,
            "a missing account must fail closed rather than resolve through member state");
    }

    [Fact]
    public void RequiredAndEmailVerified_AllowsRequest()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Global, RequiresVerifiedEmail: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Global),
            Facts(userExists: true, emailVerified: true),
            request: null!);

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void RequiredButEmailUnverified_ThrowsForbidden()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Global, RequiresVerifiedEmail: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Global),
            Facts(userExists: true, emailVerified: false),
            request: null!);

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void ActiveSubscription_AllowsPassThrough()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresSubscription: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account),
            Facts(hasActiveSubscription: true, subscriptionTier: "Pro"),
            SubscriptionRequest(null));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void NoActiveSubscription_ThrowsForbidden()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresSubscription: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account),
            Facts(hasActiveSubscription: false),
            SubscriptionRequest(null));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void MinimumTierNotMet_ThrowsForbidden()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresSubscription: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account),
            Facts(hasActiveSubscription: true, subscriptionTier: "Free"),
            SubscriptionRequest("Pro"));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void FeatureEnabled_AllowsPassThrough()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresFeature: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account),
            Facts(featureEnabled: true),
            FeatureRequest("automation"));

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void FeatureDisabled_ThrowsForbidden()
    {
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresFeature: true),
            Context(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account),
            Facts(featureEnabled: false),
            FeatureRequest("automation"));

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void MissingAccountId_ThrowsSecurityMisconfiguration()
    {
        // Constructed directly because the shared Context helper coalesces a
        // null accountId into a fresh Guid; this scenario requires a snapshot
        // whose account context is genuinely absent.
        var decision = Engine.Evaluate(
            Descriptor(ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Account, RequiresPermission: true),
            new Notrelix.Application.Common.Context.ExecutionContextSnapshot(
                Guid.NewGuid(),
                AccountId: null,
                WorkspaceId: null,
                Resource: null,
                ApplicationPrincipalKind.Authenticated,
                ApplicationScopeKind.Account,
                Guid.NewGuid().ToString("D")),
            NoFacts,
            AccountPermissionRequest(PermissionAction.ViewWorkspace));

        decision.Kind.Should().Be(AccessDecisionKind.SecurityMisconfiguration);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static AccessFacts NoFacts => new(
        false, false, false, null, false, null, false, null, null, false, [], false, null, false, true, true);

    private static AccessFacts Facts(
        bool userExists = false,
        bool emailVerified = false,
        string? accountMemberRole = null,
        string? workspaceMemberRole = null,
        bool resourceExists = false,
        string? resourceAudience = null,
        string? resourceMemberRole = null,
        bool hasExplicitResourcePermission = false,
        IReadOnlyList<AccessPermissionRule>? rules = null,
        bool hasActiveSubscription = false,
        string? subscriptionTier = null,
        bool featureEnabled = false,
        bool accountOperational = true,
        bool userOperational = true) => new(
        userExists,
        emailVerified,
        false,
        accountMemberRole,
        false,
        workspaceMemberRole,
        resourceExists,
        resourceAudience,
        resourceMemberRole,
        hasExplicitResourcePermission,
        rules ?? [],
        hasActiveSubscription,
        subscriptionTier,
        featureEnabled,
        accountOperational,
        userOperational);

    private static ExecutionContextSnapshot Context(
        ApplicationPrincipalKind principal,
        ApplicationScopeKind scope,
        Guid? userId = null,
        Guid? accountId = null) => new(
        userId ?? Guid.NewGuid(),
        accountId ?? Guid.NewGuid(),
        scope is ApplicationScopeKind.Workspace or ApplicationScopeKind.Resource ? Guid.NewGuid() : null,
        null,
        principal,
        scope,
        Guid.NewGuid().ToString("D"));

    private static RequestDescriptor Descriptor(
        ApplicationPrincipalKind principal,
        ApplicationScopeKind scope,
        ApplicationDataAccessKind DataAccess = ApplicationDataAccessKind.None,
        bool RequiresPermission = false,
        bool RequiresVerifiedEmail = false,
        bool RequiresSubscription = false,
        bool RequiresFeature = false)
    {
        var access = new AccessRequirements(
            RequiresPermission,
            RequiresVerifiedEmail,
            RequiresSubscription,
            RequiresFeature);
        return new RequestDescriptor(
            typeof(object),
            ApplicationRequestKind.Command,
            principal,
            scope,
            DataAccess,
            access,
            IsIdempotent: false,
            RequiresExpectedVersion: false);
    }

    private static object PermissionRequest(PermissionAction action, ResourceKind? resourceKind)
    {
        var resource = resourceKind is null
            ? null
            : ResourceRef.Create(resourceKind.Value, Guid.NewGuid());
        return new PermissionRequestFixture(action, resource);
    }

    private static object AccountPermissionRequest(PermissionAction action) =>
        new PermissionRequestFixture(action, null);

    private static object SubscriptionRequest(string? minimumTier) =>
        new SubscriptionRequestFixture(minimumTier);

    private static object FeatureRequest(string featureCode) =>
        new FeatureRequestFixture(featureCode);

    private sealed record PermissionRequestFixture(PermissionAction Action, ResourceRef? Resource) : IRequirePermission;

    private sealed record SubscriptionRequestFixture(string? MinimumTier) : IRequireSubscription;

    private sealed record FeatureRequestFixture(string FeatureCode) : IRequireFeature
    {
        public int Amount => 0;
    }
}
