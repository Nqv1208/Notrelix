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
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), workspaceId, "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"), 1, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();

        invitation.Accept(userId, DateTimeOffset.UtcNow);

        invitation.Status.Should().Be(WorkspaceInvitationStatus.Accepted);
        invitation.DomainEvents.Should().ContainSingle(e => e is WorkspaceInvitationAcceptedDomainEvent);
    }

    [Fact]
    public void Accept_ShouldThrow_WhenExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"), 1, Guid.NewGuid(), now, TimeSpan.FromDays(1));

        Action act = () => invitation.Accept(Guid.NewGuid(), now.AddDays(2));

        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation has expired.");
    }

    [Fact]
    public void Accept_ShouldThrow_WithoutMutating_WhenExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"), 1, Guid.NewGuid(), now, TimeSpan.FromDays(1));

        Action act = () => invitation.Accept(Guid.NewGuid(), now.AddDays(2));

        act.Should().Throw<BusinessRuleException>();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        invitation.DomainEvents.Should().NotContain(e => e is WorkspaceInvitationExpiredDomainEvent);
    }

    [Fact]
    public void Expire_ShouldSucceed_WhenPending_AndUseNullActor()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"), 1, Guid.NewGuid(), DateTimeOffset.UtcNow);

        invitation.Expire(DateTimeOffset.UtcNow);

        invitation.Status.Should().Be(WorkspaceInvitationStatus.Expired);
        invitation.UpdatedBy.Should().BeNull("expire should use null actor, not Guid.Empty");
        invitation.DomainEvents.Should().Contain(e => e is WorkspaceInvitationExpiredDomainEvent);
    }

    [Fact]
    public void Expire_ShouldDoNothing_WhenAlreadyExpired()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"), 1, Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.Expire(DateTimeOffset.UtcNow);
        ((IHasDomainEvents)invitation).ClearDomainEvents();

        invitation.Expire(DateTimeOffset.UtcNow);

        invitation.Status.Should().Be(WorkspaceInvitationStatus.Expired);
        invitation.DomainEvents.Should().NotContain(e => e is WorkspaceInvitationExpiredDomainEvent);
    }

    [Fact]
    public void Accept_ShouldThrow_WhenDeleted()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"), 1, Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => invitation.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Revoke_ShouldThrow_WhenDeleted()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"), 1, Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => invitation.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Invitation_ExpiredExactlyAtExpiresAt()
    {
        var now = DateTimeOffset.UtcNow;
        var expiry = TimeSpan.FromHours(1);
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"), 1, Guid.NewGuid(), now, expiry);

        var acceptedAt = now.Add(expiry);
        Action act = () => invitation.Accept(Guid.NewGuid(), acceptedAt);

        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation has expired.");
    }

    [Fact]
    public void Accept_ShouldThrow_WhenRevoked()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"), 1, Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => invitation.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation is not pending.");
    }

    [Fact]
    public void Accept_ShouldThrow_WhenAlreadyExpiredState()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"), 1, Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.Expire(DateTimeOffset.UtcNow);

        Action act = () => invitation.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation is not pending.");
    }

    [Fact]
    public void Resend_ShouldIncrementTokenGeneration_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = WorkspaceInvitation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test@example.com",
            WorkspaceRole.Member,
            InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"),
            1,
            Guid.NewGuid(),
            now);
        var actor = Guid.NewGuid();

        invitation.Resend(
            InvitationTokenHash.Create("b1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"),
            1,
            now.AddMinutes(1),
            TimeSpan.FromDays(7),
            actor);

        invitation.TokenGeneration.Should().Be(2);
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        invitation.DomainEvents.Should().ContainSingle(e => e is WorkspaceInvitationResentDomainEvent);
    }

    [Fact]
    public void Decline_ShouldSucceed_AndRaiseEvent()
    {
        var invitation = WorkspaceInvitation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test@example.com",
            WorkspaceRole.Member,
            InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"),
            1,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        var actor = Guid.NewGuid();

        invitation.Decline(actor, DateTimeOffset.UtcNow);

        invitation.Status.Should().Be(WorkspaceInvitationStatus.Declined);
        invitation.DomainEvents.Should().ContainSingle(e => e is WorkspaceInvitationDeclinedDomainEvent);
    }

    [Fact]
    public void Decline_ShouldThrow_WhenNotPending()
    {
        var invitation = WorkspaceInvitation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test@example.com",
            WorkspaceRole.Member,
            InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"),
            1,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        invitation.Expire(DateTimeOffset.UtcNow);

        var act = () => invitation.Decline(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation is not pending.");
    }

    [Fact]
    public void Decline_ShouldThrow_WhenDeleted()
    {
        var invitation = WorkspaceInvitation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test@example.com",
            WorkspaceRole.Member,
            InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"),
            1,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        invitation.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => invitation.Decline(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void ChangeRole_ShouldSucceed_AndRaiseEvent()
    {
        var invitation = WorkspaceInvitation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test@example.com",
            WorkspaceRole.Member,
            InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"),
            1,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        var actor = Guid.NewGuid();

        invitation.ChangeRole(WorkspaceRole.Admin, actor, DateTimeOffset.UtcNow);

        invitation.Role.Should().Be(WorkspaceRole.Admin);
        invitation.DomainEvents.Should().ContainSingle(e => e is WorkspaceInvitationRoleChangedDomainEvent);
    }

    [Fact]
    public void ChangeRole_WhenSameRole_ShouldBeNoOp()
    {
        var invitation = WorkspaceInvitation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test@example.com",
            WorkspaceRole.Member,
            InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"),
            1,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        ((IHasDomainEvents)invitation).ClearDomainEvents();

        invitation.ChangeRole(WorkspaceRole.Member, Guid.NewGuid(), DateTimeOffset.UtcNow);

        invitation.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeRole_ShouldThrow_WhenNotPending()
    {
        var invitation = WorkspaceInvitation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test@example.com",
            WorkspaceRole.Member,
            InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"),
            1,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        invitation.Expire(DateTimeOffset.UtcNow);

        var act = () => invitation.ChangeRole(WorkspaceRole.Admin, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation is not pending.");
    }

    [Fact]
    public void Create_ShouldRejectOwnerRole()
    {
        var act = () => WorkspaceInvitation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test@example.com",
            WorkspaceRole.Owner,
            InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"),
            1,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>().WithMessage("*owner*");
    }
}
