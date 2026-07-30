using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceInvitationVersionTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private WorkspaceInvitation CreatePendingInvitation()
    {
        return WorkspaceInvitation.Create(
            _accountId,
            _workspaceId,
            "test@example.com",
            WorkspaceRole.Member,
            InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"),
            1,
            _actorId,
            _now);
    }

    [CoversMutation(typeof(WorkspaceInvitation), "Accept(System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Accept_ShouldIncrementVersion()
    {
        var invitation = CreatePendingInvitation();
        ((IHasDomainEvents)invitation).ClearDomainEvents();
        var version = invitation.Version;

        invitation.Accept(Guid.NewGuid(), _now);

        invitation.Version.Should().Be(version + 1);
        invitation.DomainEvents.Should().Contain(e => e is WorkspaceInvitationAcceptedDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceInvitation), "Decline(System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Decline_ShouldIncrementVersion()
    {
        var invitation = CreatePendingInvitation();
        ((IHasDomainEvents)invitation).ClearDomainEvents();
        var version = invitation.Version;

        invitation.Decline(_actorId, _now);

        invitation.Version.Should().Be(version + 1);
        invitation.DomainEvents.Should().Contain(e => e is WorkspaceInvitationDeclinedDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceInvitation), "ChangeRole(Notrelix.Domain.Workspaces.Members.WorkspaceRole,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void ChangeRole_ShouldIncrementVersion()
    {
        var invitation = CreatePendingInvitation();
        ((IHasDomainEvents)invitation).ClearDomainEvents();
        var version = invitation.Version;

        invitation.ChangeRole(WorkspaceRole.Admin, _actorId, _now);

        invitation.Version.Should().Be(version + 1);
        invitation.DomainEvents.Should().Contain(e => e is WorkspaceInvitationRoleChangedDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceInvitation), "Expire(System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Expire_ShouldIncrementVersion()
    {
        var invitation = CreatePendingInvitation();
        ((IHasDomainEvents)invitation).ClearDomainEvents();
        var version = invitation.Version;

        invitation.Expire(_now);

        invitation.Version.Should().Be(version + 1);
        invitation.DomainEvents.Should().Contain(e => e is WorkspaceInvitationExpiredDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceInvitation), "Revoke(System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Revoke_ShouldIncrementVersion()
    {
        var invitation = CreatePendingInvitation();
        ((IHasDomainEvents)invitation).ClearDomainEvents();
        var version = invitation.Version;

        invitation.Revoke(_actorId, _now);

        invitation.Version.Should().Be(version + 1);
        invitation.DomainEvents.Should().Contain(e => e is WorkspaceInvitationRevokedDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceInvitation), "Resend(Notrelix.Domain.Workspaces.Invitations.InvitationTokenHash,System.Int32,System.DateTimeOffset,System.TimeSpan,System.Guid)", MutationScenario.Version)]
    [Fact]
    public void Resend_ShouldIncrementVersion()
    {
        var invitation = CreatePendingInvitation();
        ((IHasDomainEvents)invitation).ClearDomainEvents();
        var version = invitation.Version;

        invitation.Resend(
            InvitationTokenHash.Create("b1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"),
            1,
            _now,
            TimeSpan.FromDays(7),
            _actorId);

        invitation.Version.Should().Be(version + 1);
        invitation.DomainEvents.Should().Contain(e => e is WorkspaceInvitationResentDomainEvent);
    }

}
