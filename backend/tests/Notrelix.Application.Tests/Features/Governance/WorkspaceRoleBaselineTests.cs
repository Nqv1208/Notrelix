using Notrelix.Domain.Governance.Permissions;
using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// WG-DEBT-002: the workspace-role baseline is one typed table. Every <see cref="PermissionAction"/>
/// must be classified (a new action fails here until it is), and the real engine must match every
/// Allow/Deny cell for every workspace role (and every Account role for Account-scoped requests).
/// Level-based cells are decided on the resource level and are proven by the Board/Page/Space
/// matrices; here they are pinned to the actions that carry a level vocabulary.
/// </summary>
public sealed class WorkspaceRoleBaselineTests
{
    private static readonly string[] Roles = Enum.GetNames<WorkspaceRole>();

    private static readonly PermissionAction[] ExpectedLevelBased =
    [
        PermissionAction.ViewBoard, PermissionAction.ManageBoardPermission, PermissionAction.CreateBoardView,
        PermissionAction.UpdateBoardView, PermissionAction.ShareBoardView, PermissionAction.CreateField,
        PermissionAction.UpdateField, PermissionAction.DeleteField, PermissionAction.CreateItem,
        PermissionAction.UpdateItem, PermissionAction.MoveItem, PermissionAction.AssignItem,
        PermissionAction.ManageBoard, PermissionAction.ViewPage, PermissionAction.UpdatePage,
        PermissionAction.DeletePage, PermissionAction.ArchivePage, PermissionAction.SharePage,
        PermissionAction.ManagePagePermission, PermissionAction.CreateComment,
    ];

    public static TheoryData<PermissionAction> AllActions()
    {
        var data = new TheoryData<PermissionAction>();
        foreach (var action in Enum.GetValues<PermissionAction>())
        {
            data.Add(action);
        }

        return data;
    }

    [Fact]
    public void BaselineRoleNames_AreExactlyTheWorkspaceRoles()
    {
        WorkspaceRoleBaseline.RoleNames.Should().BeEquivalentTo(Roles);
    }

    [Fact]
    public void EveryPermissionAction_IsClassified()
    {
        var all = Enum.GetValues<PermissionAction>();

        WorkspaceRoleBaseline.ClassifiedActions.Should().BeEquivalentTo(all, "a new PermissionAction must be classified in the baseline table");
        all.Should().HaveCount(39);
    }

    [Theory]
    [MemberData(nameof(AllActions))]
    public void Owner_IsAlwaysAllowed(PermissionAction action)
    {
        WorkspaceRoleBaseline.Decide("Owner", action).Should().Be(BaselineDecision.Allow);
        Evaluate(action, "Owner").Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Theory]
    [MemberData(nameof(AllActions))]
    public void Engine_MatchesEveryWorkspaceRoleCell_OnAWorkspaceRequest(PermissionAction action)
    {
        foreach (var role in Roles)
        {
            var cell = WorkspaceRoleBaseline.Decide(role, action);
            var decision = Evaluate(action, role);

            var expected = cell switch
            {
                BaselineDecision.Allow => AccessDecisionKind.Allowed,
                // A level-based cell has no level on a workspace resource: denied, except the
                // pre-existing member-wide ViewBoard read on a non-Board resource.
                BaselineDecision.LevelBased when action == PermissionAction.ViewBoard => AccessDecisionKind.Allowed,
                _ => AccessDecisionKind.Forbidden,
            };

            decision.Kind.Should().Be(expected, $"{role} x {action} is {cell}");
        }
    }

    [Fact]
    public void LevelBasedCells_AreOnlyTheBoardPageAndCommentVocabulary()
    {
        var levelBased = Enum.GetValues<PermissionAction>()
            .Where(action => Roles.Any(role => WorkspaceRoleBaseline.Decide(role, action) == BaselineDecision.LevelBased))
            .ToArray();

        levelBased.Should().BeEquivalentTo(ExpectedLevelBased);
    }

    [Theory]
    [InlineData("Owner", PermissionAction.CreateWorkspace, true)]
    [InlineData("Admin", PermissionAction.CreateWorkspace, true)]
    [InlineData("Member", PermissionAction.CreateWorkspace, false)]
    [InlineData("Owner", PermissionAction.ManageAccount, true)]
    [InlineData("Admin", PermissionAction.ManageAccount, true)]
    [InlineData("Member", PermissionAction.ManageAccount, false)]
    [InlineData("Owner", PermissionAction.ViewWorkspace, true)]
    [InlineData("Admin", PermissionAction.ViewWorkspace, true)]
    [InlineData("Member", PermissionAction.ViewWorkspace, true)]
    [InlineData("Member", PermissionAction.ManageWorkspace, false)]
    [InlineData("Admin", PermissionAction.ManageSpaces, false)]
    public void AccountScopedRequests_UseTheAccountRoleBaseline(string accountRole, PermissionAction action, bool allowed)
    {
        var decision = Evaluate(action, workspaceRole: null, accountRole: accountRole, scope: ApplicationScopeKind.Account);

        decision.Kind.Should().Be(allowed ? AccessDecisionKind.Allowed : AccessDecisionKind.Forbidden);
    }

    // The five accepted changes (2026-10-23) and the rows that must stay as they were.
    [Theory]
    [InlineData(PermissionAction.InviteMember, "Admin", true)]
    [InlineData(PermissionAction.InviteMember, "Member", false)]
    [InlineData(PermissionAction.InviteMember, "Guest", false)]
    [InlineData(PermissionAction.RemoveMember, "Admin", true)]
    [InlineData(PermissionAction.RemoveMember, "Member", false)]
    [InlineData(PermissionAction.ManageWorkspaceSettings, "Admin", true)]
    [InlineData(PermissionAction.ManageWorkspaceSettings, "Member", false)]
    [InlineData(PermissionAction.ManageApiTokens, "Owner", true)]
    [InlineData(PermissionAction.ManageApiTokens, "Admin", false)]
    [InlineData(PermissionAction.ManageApiTokens, "Member", false)]
    [InlineData(PermissionAction.ManageApiTokens, "Guest", false)]
    [InlineData(PermissionAction.CreateBoard, "Admin", true)]
    [InlineData(PermissionAction.CreateBoard, "Member", true)]
    [InlineData(PermissionAction.CreateBoard, "Guest", false)]
    [InlineData(PermissionAction.ChangeMemberRole, "Admin", false)]
    [InlineData(PermissionAction.ManageWorkspace, "Admin", false)]
    [InlineData(PermissionAction.DeleteWorkspace, "Admin", false)]
    [InlineData(PermissionAction.ViewMembers, "Guest", true)]
    public void WorkspaceRootRows_AfterTheRoleBaselineDecision(PermissionAction action, string role, bool allowed)
    {
        Evaluate(action, role).Kind.Should().Be(allowed ? AccessDecisionKind.Allowed : AccessDecisionKind.Forbidden);
    }

    [Theory]
    [InlineData(PermissionAction.CreateBoard, "other.resource")]
    [InlineData(PermissionAction.InviteMember, "integrations.calendar-integration")]
    [InlineData(PermissionAction.ManageSpaces, "work-management.form")]
    [InlineData(PermissionAction.ManageIntegrations, "other.resource")]
    public void AllowCells_DoNotWidenToAnotherResourceKind(PermissionAction action, string kind)
    {
        var workspaceId = Guid.NewGuid();
        var request = new BaselineProbe(
            workspaceId, action, ResourceRef.Create(ResourceKind.Create(kind), workspaceId, workspaceId));

        Evaluate(request, "Admin").Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Theory]
    [InlineData("integrations.calendar-integration")]
    [InlineData("automation.rule")]
    public void WorkspaceSettings_ApplyToTheAutomationAndIntegrationResources(string kind)
    {
        var workspaceId = Guid.NewGuid();
        var request = new BaselineProbe(
            workspaceId, PermissionAction.ManageWorkspaceSettings, ResourceRef.Create(ResourceKind.Create(kind), workspaceId, workspaceId));

        Evaluate(request, "Admin").Kind.Should().Be(AccessDecisionKind.Allowed);
        Evaluate(request, "Member").Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void FirstPriorityDenyRule_StillBeatsTheBaseline()
    {
        Evaluate(PermissionAction.CreateBoard, "Member", rules: [new AccessPermissionRule(0, "Deny")])
            .Kind.Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void FirstPriorityAllowRule_StillGoesThroughTheGrantCeiling_NotTheBaseline()
    {
        Evaluate(PermissionAction.ManageWorkspace, "Member", rules: [new AccessPermissionRule(0, "Allow")])
            .Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    private static AccessDecision Evaluate(
        PermissionAction action,
        string? workspaceRole,
        string accountRole = "Owner",
        ApplicationScopeKind scope = ApplicationScopeKind.Workspace,
        AccessPermissionRule[]? rules = null)
    {
        var workspaceId = Guid.NewGuid();
        return Evaluate(
            new BaselineProbe(
                workspaceId,
                action,
                ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), workspaceId, workspaceId)),
            workspaceRole,
            accountRole,
            scope,
            rules);
    }

    private static AccessDecision Evaluate(
        BaselineProbe request,
        string? workspaceRole,
        string accountRole = "Owner",
        ApplicationScopeKind scope = ApplicationScopeKind.Workspace,
        AccessPermissionRule[]? rules = null)
    {
        var workspaceId = request.WorkspaceId;
        var descriptor = RequestDescriptorValidator.Create(typeof(BaselineProbe)) with { Scope = scope };
        var snapshot = new ExecutionContextSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            scope == ApplicationScopeKind.Account ? null : workspaceId,
            null,
            ApplicationPrincipalKind.Authenticated,
            scope,
            Guid.NewGuid().ToString("D"));
        var facts = new AccessFacts(
            UserExists: true,
            EmailVerified: true,
            AccountExists: true,
            AccountMemberRole: accountRole,
            WorkspaceExists: true,
            WorkspaceMemberRole: workspaceRole,
            ResourceExists: true,
            ResourceAudience: null,
            ResourceMemberRole: null,
            HasExplicitResourcePermission: false,
            PermissionRules: rules ?? [],
            SubscriptionRequirementSatisfied: false,
            FeatureEnabled: false,
            ActiveResourcePermissionRank: null,
            TargetPermissionRank: null);

        return new AccessPolicyEngine().Evaluate(descriptor, snapshot, facts, request);
    }

    public sealed record BaselineProbe(Guid WorkspaceId, PermissionAction Action, ResourceRef Resource)
        : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission;
}
