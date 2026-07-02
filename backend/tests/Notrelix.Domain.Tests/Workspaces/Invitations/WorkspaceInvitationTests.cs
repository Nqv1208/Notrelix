using FluentAssertions;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceInvitationTests
{
    [Fact]
    public void Accept_ShouldSucceed_WhenPendingAndNotExpired()
    {
        var workspaceId = Guid.NewGuid();
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), workspaceId, "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();

        invitation.Accept(userId, DateTimeOffset.UtcNow);

        invitation.Status.Should().Be(WorkspaceInvitationStatus.Accepted);
        invitation.DomainEvents.Should().ContainSingle(e => e is WorkspaceInvitationAcceptedDomainEvent);
    }

    [Fact]
    public void Accept_ShouldThrow_WhenExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), now, TimeSpan.FromDays(1));

        Action act = () => invitation.Accept(Guid.NewGuid(), now.AddDays(2));

        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation has expired.");
    }

    [Fact]
    public void Accept_ShouldThrow_WithoutMutating_WhenExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), now, TimeSpan.FromDays(1));

        Action act = () => invitation.Accept(Guid.NewGuid(), now.AddDays(2));

        act.Should().Throw<BusinessRuleException>();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        invitation.DomainEvents.Should().NotContain(e => e is WorkspaceInvitationExpiredDomainEvent);
    }

    [Fact]
    public void Expire_ShouldSucceed_WhenPending_AndUseNullActor()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        invitation.Expire(DateTimeOffset.UtcNow);

        invitation.Status.Should().Be(WorkspaceInvitationStatus.Expired);
        invitation.UpdatedBy.Should().BeNull("expire should use null actor, not Guid.Empty");
        invitation.DomainEvents.Should().Contain(e => e is WorkspaceInvitationExpiredDomainEvent);
    }

    [Fact]
    public void Expire_ShouldDoNothing_WhenAlreadyExpired()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.Expire(DateTimeOffset.UtcNow);
        invitation.ClearDomainEvents();

        invitation.Expire(DateTimeOffset.UtcNow);

        invitation.Status.Should().Be(WorkspaceInvitationStatus.Expired);
        invitation.DomainEvents.Should().NotContain(e => e is WorkspaceInvitationExpiredDomainEvent);
    }

    [Fact]
    public void Accept_ShouldThrow_WhenDeleted()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => invitation.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Revoke_ShouldThrow_WhenDeleted()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => invitation.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Invitation_ExpiredExactlyAtExpiresAt()
    {
        var now = DateTimeOffset.UtcNow;
        var expiry = TimeSpan.FromHours(1);
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), now, expiry);

        var acceptedAt = now.Add(expiry); // exactly at ExpiresAt
        Action act = () => invitation.Accept(Guid.NewGuid(), acceptedAt);

        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation has expired.");
    }

    [Fact]
    public void Accept_ShouldThrow_WhenRevoked()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => invitation.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation is not pending.");
    }

    [Fact]
    public void Accept_ShouldThrow_WhenAlreadyExpiredState()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("token"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.Expire(DateTimeOffset.UtcNow);

        Action act = () => invitation.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation is not pending.");
    }
}
