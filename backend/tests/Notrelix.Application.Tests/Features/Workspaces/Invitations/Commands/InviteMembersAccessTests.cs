using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMembers;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands;

/// <summary>
/// Who may send a batch, decided by the real AccessPolicyEngine with the real InviteMembersCommand descriptor (the
/// pipeline's AccessControl behavior runs exactly this before the handler): the batch needs InviteMember on the
/// Workspace like a single invitation, so a Member or a Guest is forbidden for the whole request, a non-member is told
/// it does not exist, and the handler is never reached.
/// </summary>
public sealed class InviteMembersAccessTests
{
    private static readonly RequestDescriptor Descriptor = RequestDescriptorValidator.Create(typeof(InviteMembersCommand));

    private static InviteMembersCommand Batch() => new(Guid.NewGuid(), [new MemberInvitationEntry("a@x.com", "Member")]);

    [Fact]
    public void TheBatchNeedsTheSameActionAsASingleInvitation()
    {
        ((IRequirePermission)Batch()).Action.Should().Be(PermissionAction.InviteMember);
        ((IRequirePermission)new InviteMemberCommand(Guid.NewGuid(), "a@x.com", WorkspaceRole.Member)).Action.Should().Be(PermissionAction.InviteMember);
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    public void OwnerAndAdmin_AreAllowed(string role) =>
        Evaluate(role).Kind.Should().Be(AccessDecisionKind.Allowed);

    [Theory]
    [InlineData("Member")]
    [InlineData("Guest")]
    public void MemberAndGuest_AreForbiddenForTheWholeBatch(string role) =>
        Evaluate(role).Kind.Should().Be(AccessDecisionKind.Forbidden);

    [Fact]
    public void ANonMember_IsToldTheWorkspaceDoesNotExist() =>
        Evaluate(null).Kind.Should().Be(AccessDecisionKind.NotFound);

    private static AccessDecision Evaluate(string? workspaceRole) =>
        new AccessPolicyEngine().Evaluate(
            Descriptor,
            new ExecutionContextSnapshot(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
                ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Workspace, Guid.NewGuid().ToString("D")),
            new AccessFacts(
                UserExists: true, EmailVerified: true, AccountExists: true, AccountMemberRole: "Member",
                WorkspaceExists: true, WorkspaceMemberRole: workspaceRole, ResourceExists: true, ResourceAudience: null,
                ResourceMemberRole: null, HasExplicitResourcePermission: false, PermissionRules: [],
                SubscriptionRequirementSatisfied: false, FeatureEnabled: false, ActiveResourcePermissionRank: null,
                TargetPermissionRank: null),
            Batch());
}
