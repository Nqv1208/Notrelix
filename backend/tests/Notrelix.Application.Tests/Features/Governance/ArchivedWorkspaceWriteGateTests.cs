using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Public.Authorization;
using Notrelix.Application.Features.Workspaces.Members.Commands.LeaveWorkspace;
using Notrelix.Application.Features.Workspaces.Public.Lifecycle;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.DeleteWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.RestoreWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UnarchiveWorkspace;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// An archived workspace is read-only for every member (product workspaces.md, UC-WSP-D01): once every other rule
/// allowed the request, a write is refused with <c>workspace.archived</c>; reads and the workspace lifecycle requests
/// pass, and a caller who is not a member still gets the identical NotFound.
/// </summary>
public sealed class ArchivedWorkspaceWriteGateTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly ResourceRef WorkspaceRef =
        ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    [InlineData("Member")]
    [InlineData("Guest")]
    public void AWrite_OnAnArchivedWorkspace_IsRefusedWithTheArchivedCode_ForEveryMember(string role)
    {
        // ViewWorkspace is allowed to every role, so the archived gate is the first rule that refuses.
        var decision = Evaluate(new WriteProbe(WorkspaceId, PermissionAction.ViewWorkspace, WorkspaceRef), role, archived: true);

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
        decision.Code.Should().Be(AccessDenialCodes.WorkspaceArchived).And.Be("workspace.archived");
        decision.Message.Should().Be("This workspace is archived. It is read-only until an owner unarchives it.");
    }

    [Fact]
    public void AWrite_ThatTheRoleAlreadyCannotDo_KeepsTheNormalForbidden_WithoutTheArchivedCode()
    {
        // The role refusal comes first, so a Member never learns the workspace is archived from a request it could not make.
        var decision = Evaluate(new WriteProbe(WorkspaceId, PermissionAction.ManageWorkspace, WorkspaceRef), "Member", archived: true);

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
        decision.Code.Should().BeNull();
    }

    [Fact]
    public void AWrite_OnAnActiveWorkspace_IsNotAffected() =>
        Evaluate(new WriteProbe(WorkspaceId, PermissionAction.ViewWorkspace, WorkspaceRef), "Owner", archived: false)
            .Kind.Should().Be(AccessDecisionKind.Allowed);

    [Theory]
    [InlineData("Owner")]
    [InlineData("Member")]
    [InlineData("Guest")]
    public void ARead_OnAnArchivedWorkspace_StaysAllowed(string role) =>
        Evaluate(new ReadProbe(WorkspaceId, PermissionAction.ViewWorkspace, WorkspaceRef), role, archived: true)
            .Kind.Should().Be(AccessDecisionKind.Allowed);

    [Theory]
    [InlineData(null)]
    public void ANonMember_OfAnArchivedWorkspace_GetsTheIdenticalNotFound(string? role)
    {
        var decision = Evaluate(new WriteProbe(WorkspaceId, PermissionAction.ViewWorkspace, WorkspaceRef), role, archived: true);

        decision.Kind.Should().Be(AccessDecisionKind.NotFound);
        decision.Message.Should().Be("Resource not found.");
        decision.Code.Should().BeNull();
    }

    public static TheoryData<IWorkspaceRequest> LifecycleRequests() => new()
    {
        new ArchiveWorkspaceCommand(WorkspaceId, 1),
        new UnarchiveWorkspaceCommand(WorkspaceId, 1),
        new DeleteWorkspaceCommand(WorkspaceId, 1),
        new LeaveWorkspaceCommand(WorkspaceId),
    };

    [Theory]
    [MemberData(nameof(LifecycleRequests))]
    public void TheWorkspaceLifecycleRequests_PassOnAnArchivedWorkspace_ForTheOwner(IWorkspaceRequest request)
    {
        request.Should().BeAssignableTo<IWorkspaceLifecycleRequest>();
        Evaluate(request, "Owner", archived: true).Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void Restore_IsALifecycleRequest_AndAnArchivedWorkspaceDoesNotRefuseIt()
    {
        var restore = new RestoreWorkspaceCommand(WorkspaceId, 1);

        restore.Should().BeAssignableTo<IWorkspaceLifecycleRequest>();
        Evaluate(restore, "Owner", archived: true).Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void AMember_CanLeaveAnArchivedWorkspace() =>
        Evaluate(new LeaveWorkspaceCommand(WorkspaceId), "Member", archived: true).Kind.Should().Be(AccessDecisionKind.Allowed);

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    [InlineData("Member")]
    [InlineData("Guest")]
    public void ABoardWrite_OnAnArchivedWorkspace_IsRefused_EvenTheBoardLifecycleOnes(string role)
    {
        // Archiving or unarchiving a Board is a write inside the workspace, so it is refused like any other.
        var decision = Evaluate(new BoardLifecycleProbe(WorkspaceId, PermissionAction.ViewWorkspace, WorkspaceRef), role, archived: true);

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
        decision.Code.Should().Be(AccessDenialCodes.WorkspaceArchived);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Guest")]
    public void APersonalBoardWrite_OnAnArchivedWorkspace_StaysAllowed(string role) =>
        Evaluate(new PersonalBoardProbe(WorkspaceId, PermissionAction.ViewWorkspace, WorkspaceRef), role, archived: true)
            .Kind.Should().Be(AccessDecisionKind.Allowed);

    [Theory]
    [InlineData("Admin")]
    [InlineData("Member")]
    [InlineData("Guest")]
    public void APersonalWorkspaceWrite_OnAnArchivedWorkspace_StaysAllowed(string role) =>
        Evaluate(new PersonalWorkspaceProbe(WorkspaceId, PermissionAction.ViewWorkspace, WorkspaceRef), role, archived: true)
            .Kind.Should().Be(AccessDecisionKind.Allowed);

    [Fact]
    public void AnAccountScopedWrite_IsNotAffectedByAWorkspaceArchivedFact() =>
        Evaluate(new AccountWriteProbe(PermissionAction.ViewWorkspace), "Owner", archived: true, ApplicationScopeKind.Account)
            .Kind.Should().Be(AccessDecisionKind.Allowed);

    private static AccessDecision Evaluate(
        object request, string? workspaceRole, bool archived, ApplicationScopeKind scope = ApplicationScopeKind.Workspace)
    {
        var descriptor = RequestDescriptorValidator.Create(request.GetType()) with { Scope = scope };
        var snapshot = new ExecutionContextSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), WorkspaceId, null,
            ApplicationPrincipalKind.Authenticated, scope, Guid.NewGuid().ToString("D"));
        var facts = new AccessFacts(
            UserExists: true,
            EmailVerified: true,
            AccountExists: true,
            AccountMemberRole: "Member",
            WorkspaceExists: true,
            WorkspaceMemberRole: workspaceRole,
            ResourceExists: true,
            ResourceAudience: null,
            ResourceMemberRole: null,
            HasExplicitResourcePermission: false,
            PermissionRules: [],
            SubscriptionRequirementSatisfied: false,
            FeatureEnabled: false,
            ActiveResourcePermissionRank: null,
            TargetPermissionRank: null)
        {
            WorkspaceArchived = archived,
        };

        return new AccessPolicyEngine().Evaluate(descriptor, snapshot, facts, request);
    }

    private sealed record WriteProbe(Guid WorkspaceId, PermissionAction Action, ResourceRef Resource)
        : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission;

    private sealed record BoardLifecycleProbe(Guid WorkspaceId, PermissionAction Action, ResourceRef Resource)
        : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IBoardLifecycleRequest;

    private sealed record PersonalBoardProbe(Guid WorkspaceId, PermissionAction Action, ResourceRef Resource)
        : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IPersonalBoardRequest;

    private sealed record PersonalWorkspaceProbe(Guid WorkspaceId, PermissionAction Action, ResourceRef Resource)
        : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IPersonalWorkspaceRequest;

    private sealed record AccountWriteProbe(PermissionAction Action)
        : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IAccountRequest, IRequirePermission
    {
        public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("accounts.account"), Guid.Empty);
    }

    private sealed record ReadProbe(Guid WorkspaceId, PermissionAction Action, ResourceRef Resource)
        : IQuery<Result>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission;
}
