using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Public.Lifecycle;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// Owner decision 2026-11-01: a deleted workspace is NotFound to everyone (the identical answer for a member,
/// an Admin, the Owner and a stranger, in the Workspace scope and for a resource inside it), only the restore
/// request reaches it, and deleting or restoring is Owner-only: no rule can lend it.
/// </summary>
public sealed class DeletedWorkspaceAccessTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly ResourceRef WorkspaceRef =
        ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);

    public static TheoryData<string?, ApplicationScopeKind> EveryoneOnADeletedWorkspace()
    {
        var data = new TheoryData<string?, ApplicationScopeKind>();
        foreach (var role in new string?[] { "Owner", "Admin", "Member", "Guest", null })
        {
            data.Add(role, ApplicationScopeKind.Workspace);
            data.Add(role, ApplicationScopeKind.Resource);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(EveryoneOnADeletedWorkspace))]
    public void ADeletedWorkspace_IsTheIdenticalNotFound_ForEveryRole_InBothScopes(string? role, ApplicationScopeKind scope)
    {
        var decision = Evaluate(new ReadProbe(WorkspaceId, PermissionAction.ViewWorkspace, WorkspaceRef), role, scope, workspaceExists: false);

        decision.Kind.Should().Be(AccessDecisionKind.NotFound);
        decision.Message.Should().Be("Resource not found.");
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    [InlineData("Member")]
    public void AnActiveWorkspace_IsNotAffected(string role) =>
        Evaluate(new ReadProbe(WorkspaceId, PermissionAction.ViewWorkspace, WorkspaceRef), role, ApplicationScopeKind.Workspace, workspaceExists: true)
            .Kind.Should().Be(AccessDecisionKind.Allowed);

    [Fact]
    public void Restore_ReachesTheDeletedWorkspace_ForItsOwner()
    {
        var decision = Evaluate(new RestoreProbe(WorkspaceId, PermissionAction.RestoreWorkspace, WorkspaceRef), "Owner",
            ApplicationScopeKind.Workspace, workspaceExists: false);

        decision.Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Member")]
    [InlineData("Guest")]
    [InlineData(null)]
    public void Restore_OfADeletedWorkspace_IsTheSameNotFoundForEveryoneButTheOwner(string? role) =>
        Evaluate(new RestoreProbe(WorkspaceId, PermissionAction.RestoreWorkspace, WorkspaceRef), role,
            ApplicationScopeKind.Workspace, workspaceExists: false)
            .Kind.Should().Be(AccessDecisionKind.NotFound);

    [Theory]
    [InlineData("Admin")]
    [InlineData("Member")]
    public void Restore_OfAnActiveWorkspace_IsForbiddenToANonOwner(string role) =>
        Evaluate(new RestoreProbe(WorkspaceId, PermissionAction.RestoreWorkspace, WorkspaceRef), role,
            ApplicationScopeKind.Workspace, workspaceExists: true)
            .Kind.Should().Be(AccessDecisionKind.Forbidden);

    [Theory]
    [InlineData(PermissionAction.DeleteWorkspace)]
    [InlineData(PermissionAction.RestoreWorkspace)]
    public void DeleteAndRestore_AreOwnerOnly_AndNoAllowRuleCanLendThem(PermissionAction action)
    {
        var request = new ReadProbe(WorkspaceId, action, WorkspaceRef);

        Evaluate(request, "Owner", ApplicationScopeKind.Workspace, workspaceExists: true).Kind.Should().Be(AccessDecisionKind.Allowed);
        foreach (var role in new[] { "Admin", "Member", "Guest" })
        {
            Evaluate(request, role, ApplicationScopeKind.Workspace, workspaceExists: true)
                .Kind.Should().Be(AccessDecisionKind.Forbidden, role);
            Evaluate(request, role, ApplicationScopeKind.Workspace, workspaceExists: true, rules: [new AccessPermissionRule(0, "Allow")])
                .Kind.Should().Be(AccessDecisionKind.Forbidden, role + " with an Allow rule");
        }
    }

    private static AccessDecision Evaluate(
        IWorkspaceRequest request,
        string? workspaceRole,
        ApplicationScopeKind scope,
        bool workspaceExists,
        AccessPermissionRule[]? rules = null)
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
            WorkspaceExists: workspaceExists,
            WorkspaceMemberRole: workspaceRole,
            ResourceExists: workspaceExists,
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

    private sealed record ReadProbe(Guid WorkspaceId, PermissionAction Action, ResourceRef Resource)
        : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission;

    private sealed record RestoreProbe(Guid WorkspaceId, PermissionAction Action, ResourceRef Resource)
        : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IWorkspaceReachesDeletedRequest;
}
