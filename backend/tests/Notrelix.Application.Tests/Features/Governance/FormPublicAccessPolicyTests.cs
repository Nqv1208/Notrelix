using Notrelix.Application.Features.WorkManagement.Forms.Commands.SetFormPublicAccess;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.GetFormPublicAccess;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// Switching a form public (and reading its public key) is a workspace-level right even though a form is a Board
/// child: Owner and Admin hold it, a Member or Guest never does however much they manage the Board, a Guest learns
/// nothing about the form, a custom role that removes the action loses it, and a form that is not found is NotFound.
/// </summary>
public sealed class FormPublicAccessPolicyTests
{
    private static readonly AccessPolicyEngine Engine = new();

    private static readonly ExecutionContextSnapshot Context = new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
        ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "test");

    private static RequestDescriptor Descriptor(Type request) => new(
        request, ApplicationRequestKind.Command, ApplicationPrincipalKind.Authenticated,
        ApplicationScopeKind.Resource, ApplicationDataAccessKind.Write,
        new AccessRequirements(true, false, false, false), false, false);

    public static TheoryData<IRequirePermission> Requests() => new()
    {
        new SetFormPublicAccessCommand(Guid.NewGuid(), true),
        new GetFormPublicAccessQuery(Guid.NewGuid()),
    };

    [Theory]
    [MemberData(nameof(Requests))]
    public void TheRequests_NeedWorkspaceSettingsOnTheFormResource(IRequirePermission request)
    {
        request.Action.Should().Be(PermissionAction.ManageWorkspaceSettings);
        request.Resource!.Kind.Value.Should().Be("work-management.form");
    }

    [Theory]
    [MemberData(nameof(Requests))]
    public void OwnerAndAdmin_Allowed_MemberForbidden_GuestNotFound(IRequirePermission request)
    {
        Decide(request, Facts("Owner")).Should().Be(AccessDecisionKind.Allowed);
        Decide(request, Facts("Admin")).Should().Be(AccessDecisionKind.Allowed);
        Decide(request, Facts("Member")).Should().Be(AccessDecisionKind.Forbidden);
        Decide(request, Facts("Guest")).Should().Be(AccessDecisionKind.NotFound);
    }

    [Fact]
    public void ABoardManagerWhoIsOnlyAMember_StillCannotSwitchAFormPublic()
    {
        var manager = Facts("Member") with { ResourceMemberRole = "Manager", HasExplicitResourcePermission = true };

        Decide(new SetFormPublicAccessCommand(Guid.NewGuid(), true), manager).Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void AFormThatIsNotFound_IsNotFound_ForEveryone()
    {
        foreach (var role in new[] { "Owner", "Admin", "Member", "Guest" })
            Decide(new SetFormPublicAccessCommand(Guid.NewGuid(), true), Facts(role) with { ResourceExists = false })
                .Should().Be(AccessDecisionKind.NotFound, role);
    }

    private static AccessDecisionKind Decide(IRequirePermission request, AccessFacts facts) =>
        Engine.Evaluate(Descriptor(request.GetType()), Context, facts, request).Kind;

    private static AccessFacts Facts(string role) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Member",
        WorkspaceExists: true,
        WorkspaceMemberRole: role,
        ResourceExists: true,
        ResourceAudience: null,
        ResourceMemberRole: null,
        HasExplicitResourcePermission: false,
        PermissionRules: [],
        SubscriptionRequirementSatisfied: true,
        FeatureEnabled: true,
        ActiveResourcePermissionRank: null,
        TargetPermissionRank: null)
    {
        IsBoardChild = true,
        ParentBoardId = Guid.NewGuid(),
    };
}
