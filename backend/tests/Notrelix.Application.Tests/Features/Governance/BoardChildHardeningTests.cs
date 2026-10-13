using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SaveBoardView;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// Hardening around Board-decided requests: saving the Board's shared view is
/// an Editor change, not a read; a Board child accepts only the Board-child
/// action vocabulary (an unrelated action such as a workspace-level share is
/// refused, not Editor-gated); and a Board child whose Board cannot be resolved
/// is NotFound in any scope instead of falling through to a View allowance.
/// </summary>
public sealed class BoardChildHardeningTests
{
    private static readonly AccessPolicyEngine Engine = new();
    private static readonly Guid BoardId = Guid.Parse("00000000-0000-0000-0000-0000000000b0");

    private static readonly ExecutionContextSnapshot Context = new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
        ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "test");

    private static readonly RequestDescriptor ProbeDescriptor = new(
        typeof(Probe), ApplicationRequestKind.Query, ApplicationPrincipalKind.Authenticated,
        ApplicationScopeKind.Resource, ApplicationDataAccessKind.Read,
        new AccessRequirements(true, false, false, false), false, false);

    [Fact]
    public void SavingTheSharedBoardView_NeedsEditor()
    {
        var request = new SaveBoardViewCommand(BoardId, Notrelix.Domain.WorkManagement.ViewMode.Kanban, "{}");
        var descriptor = RequestDescriptorValidator.Create(typeof(SaveBoardViewCommand));

        request.Action.Should().Be(PermissionAction.UpdateBoardView);
        Engine.Evaluate(descriptor, Context, Facts("Member", audience: "Private", explicitRank: 1), request).Kind
            .Should().Be(AccessDecisionKind.Forbidden, "a Viewer cannot change the view everyone sees");
        Engine.Evaluate(descriptor, Context, Facts("Guest", explicitRank: 1), request).Kind
            .Should().Be(AccessDecisionKind.Forbidden);
        Engine.Evaluate(descriptor, Context, Facts("Member", audience: "Private", explicitRank: 3), request).Kind
            .Should().Be(AccessDecisionKind.Allowed);
    }

    [Theory]
    [InlineData(PermissionAction.ManageWorkspace)]
    [InlineData(PermissionAction.SharePage)]
    [InlineData(PermissionAction.ManageSpaces)]
    [InlineData(PermissionAction.DeleteWorkspace)]
    public void ABoardChild_RefusesActionsOutsideTheBoardChildVocabulary(PermissionAction action)
    {
        var editor = Facts("Member", audience: "Private", explicitRank: 4) with { ParentBoardId = BoardId, IsBoardChild = true };

        Engine.Evaluate(ProbeDescriptor, Context, editor, new Probe(action, "work-management.board-item")).Kind
            .Should().Be(AccessDecisionKind.Forbidden, $"{action} is not an action on a Board child");
    }

    [Fact]
    public void ABoardChild_KeepsItsOwnVocabulary()
    {
        var editor = Facts("Member", audience: "Private", explicitRank: 3) with { ParentBoardId = BoardId, IsBoardChild = true };

        foreach (var action in new[]
                 {
                     PermissionAction.ViewBoard, PermissionAction.CreateComment, PermissionAction.CreateItem,
                     PermissionAction.UpdateItem, PermissionAction.MoveItem, PermissionAction.AssignItem,
                     PermissionAction.UpdateField, PermissionAction.DeleteField, PermissionAction.UpdateBoardView,
                     PermissionAction.ManageBoard,
                 })
        {
            Engine.Evaluate(ProbeDescriptor, Context, editor, new Probe(action, "work-management.board-item")).Kind
                .Should().Be(AccessDecisionKind.Allowed, action.ToString());
        }
    }

    [Fact]
    public void ABoardChildWithoutAResolvableBoard_IsNotFound_InAnyScope()
    {
        var facts = Facts("Member") with { IsBoardChild = true, ParentBoardId = null };
        var workspaceScope = ProbeDescriptor with { Scope = ApplicationScopeKind.Workspace };

        Engine.Evaluate(workspaceScope, Context, facts, new Probe(PermissionAction.ViewBoard, "work-management.board-item")).Kind
            .Should().Be(AccessDecisionKind.NotFound);
        Engine.Evaluate(ProbeDescriptor, Context, facts, new Probe(PermissionAction.ViewBoard, "work-management.board-item")).Kind
            .Should().Be(AccessDecisionKind.NotFound);
    }

    private static AccessFacts Facts(string role, string audience = "Workspace", int? explicitRank = null) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Member",
        WorkspaceExists: true,
        WorkspaceMemberRole: role,
        ResourceExists: true,
        ResourceAudience: audience,
        ResourceMemberRole: null,
        HasExplicitResourcePermission: explicitRank is not null,
        PermissionRules: [],
        SubscriptionRequirementSatisfied: true,
        FeatureEnabled: true,
        ActiveResourcePermissionRank: explicitRank,
        TargetPermissionRank: null);

    private sealed record Probe(PermissionAction Action, string Kind) : IRequirePermission
    {
        public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create(Kind), Guid.Parse("00000000-0000-0000-0000-0000000000c1"));
    }
}
