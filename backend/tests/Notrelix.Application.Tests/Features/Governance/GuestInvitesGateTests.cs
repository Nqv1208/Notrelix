using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.CancelInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteGuestToResource;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendResourceInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.RevokeResourceInvitation;
using Notrelix.Application.Features.Workspaces.Public.Invitations;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// The workspace setting AllowGuestInvites in the policy engine: a request that introduces a Guest is refused with
/// <c>workspace.guest_invites_off</c> once the normal rules allowed it, the answer fails closed when the fact is absent, a
/// caller who could not make the request keeps the plain refusal, and revoking or cancelling is never blocked.
/// </summary>
public sealed class GuestInvitesGateTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid BoardId = Guid.NewGuid();

    [Theory]
    [InlineData("work-management.board")]
    [InlineData("documents.page")]
    public void AResourceInvitation_IsRefusedWithTheStableCode_WhenGuestInvitesAreOff(string kind)
    {
        var decision = Evaluate(new InviteGuestToResourceCommand(kind, BoardId, "g@example.com", "Viewer"), allowed: false);

        decision.Kind.Should().Be(AccessDecisionKind.Forbidden);
        decision.Code.Should().Be(AccessDenialCodes.GuestInvitesOff).And.Be("workspace.guest_invites_off");
    }

    [Fact]
    public void AResend_IsRefused_WhenGuestInvitesAreOff() =>
        Evaluate(new ResendResourceInvitationCommand("work-management.board", BoardId, Guid.NewGuid()), allowed: false)
            .Code.Should().Be(AccessDenialCodes.GuestInvitesOff);

    [Fact]
    public void AWorkspaceInvitation_WithTheGuestRole_IsRefused_ButAMemberInvitationIsNot()
    {
        Evaluate(new InviteMemberCommand(WorkspaceId, "g@example.com", WorkspaceRole.Guest), allowed: false)
            .Code.Should().Be(AccessDenialCodes.GuestInvitesOff);
        Evaluate(new InviteMemberCommand(WorkspaceId, "m@example.com", WorkspaceRole.Member), allowed: false)
            .Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void RevokeAndCancel_AreNeverBlocked_WhenGuestInvitesAreOff()
    {
        Evaluate(new RevokeResourceInvitationCommand("work-management.board", BoardId, Guid.NewGuid()), allowed: false)
            .Kind.Should().Be(AccessDecisionKind.Allowed);
        Evaluate(new CancelInvitationCommand(WorkspaceId, Guid.NewGuid()), allowed: false)
            .Kind.Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void WhenGuestInvitesAreOn_TheResourceInvitationPasses() =>
        Evaluate(new InviteGuestToResourceCommand("work-management.board", BoardId, "g@example.com", "Viewer"), allowed: true)
            .Kind.Should().Be(AccessDecisionKind.Allowed);

    [Fact]
    public void WhenTheFactIsMissing_TheGateFailsClosed()
    {
        // Facts built without the setting (as when the workspace row cannot be read) never wave a Guest invitation through.
        var request = new InviteGuestToResourceCommand("work-management.board", BoardId, "g@example.com", "Viewer");
        var (descriptor, snapshot) = Describe(request);

        new AccessPolicyEngine().Evaluate(descriptor, snapshot, Facts("Owner"), request)
            .Code.Should().Be(AccessDenialCodes.GuestInvitesOff);
    }

    [Fact]
    public void ACallerWhoCannotMakeTheRequest_KeepsThePlainRefusal_WithoutLearningTheSetting()
    {
        var decision = Evaluate(
            new InviteGuestToResourceCommand("work-management.board", BoardId, "g@example.com", "Viewer"),
            allowed: false, role: "Guest");

        decision.Kind.Should().NotBe(AccessDecisionKind.Allowed);
        decision.Code.Should().BeNull();
    }

    [Fact]
    public void TheMarkerIsCarriedByExactlyTheRequestsThatIntroduceAGuest()
    {
        typeof(InviteGuestToResourceCommand).Should().BeAssignableTo<IGuestInvitationRequest>();
        typeof(ResendResourceInvitationCommand).Should().BeAssignableTo<IGuestInvitationRequest>();
        typeof(InviteMemberCommand).Should().BeAssignableTo<IGuestInvitationRequest>();
        typeof(RevokeResourceInvitationCommand).Should().NotBeAssignableTo<IGuestInvitationRequest>();
        typeof(CancelInvitationCommand).Should().NotBeAssignableTo<IGuestInvitationRequest>();
    }

    private static (RequestDescriptor, ExecutionContextSnapshot) Describe(object request)
    {
        var descriptor = RequestDescriptorValidator.Create(request.GetType());
        var snapshot = new ExecutionContextSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), WorkspaceId, null,
            ApplicationPrincipalKind.Authenticated, descriptor.Scope, Guid.NewGuid().ToString("D"));
        return (descriptor, snapshot);
    }

    private static AccessDecision Evaluate(object request, bool allowed, string role = "Owner")
    {
        var (descriptor, snapshot) = Describe(request);
        return new AccessPolicyEngine().Evaluate(descriptor, snapshot, Facts(role) with { GuestInvitesAllowed = allowed }, request);
    }

    private static AccessFacts Facts(string role) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Member",
        WorkspaceExists: true,
        WorkspaceMemberRole: role,
        ResourceExists: true,
        ResourceAudience: "Workspace",
        ResourceMemberRole: null,
        HasExplicitResourcePermission: true,
        PermissionRules: [],
        SubscriptionRequirementSatisfied: false,
        FeatureEnabled: false,
        ActiveResourcePermissionRank: 4,
        TargetPermissionRank: null);
}
