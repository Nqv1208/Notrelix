using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Workspaces;

[CoversAggregate(typeof(WorkspaceInvitation))]
public class WorkspaceInvitationTests
{
    [CoversMutation(typeof(WorkspaceInvitation), "Accept(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
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

    [CoversMutation(typeof(WorkspaceInvitation), "Accept(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void Accept_ShouldThrow_WhenExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"), 1, Guid.NewGuid(), now, TimeSpan.FromDays(1));

        Action act = () => invitation.Accept(Guid.NewGuid(), now.AddDays(2));

        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation has expired.");
    }

    [CoversMutation(typeof(WorkspaceInvitation), "Accept(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
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

    [CoversMutation(typeof(WorkspaceInvitation), "Expire(System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Expire_ShouldSucceed_WhenPending_AndUseNullActor()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"), 1, Guid.NewGuid(), DateTimeOffset.UtcNow);

        invitation.Expire(DateTimeOffset.UtcNow);

        invitation.Status.Should().Be(WorkspaceInvitationStatus.Expired);
        invitation.UpdatedBy.Should().BeNull("expire should use null actor, not Guid.Empty");
        invitation.DomainEvents.Should().Contain(e => e is WorkspaceInvitationExpiredDomainEvent);
    }

    [CoversMutation(typeof(WorkspaceInvitation), "Expire(System.DateTimeOffset)", MutationScenario.NoOp)]
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

    [CoversMutation(typeof(WorkspaceInvitation), "Expire(System.DateTimeOffset)", MutationScenario.Valid)]
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

    [CoversMutation(typeof(WorkspaceInvitation), "Accept(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void Accept_ShouldThrow_WhenRevoked()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"), 1, Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Action act = () => invitation.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation is not pending.");
    }

    [CoversMutation(typeof(WorkspaceInvitation), "Accept(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Accept_ShouldThrow_WhenAlreadyExpiredState()
    {
        var invitation = WorkspaceInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), "test@example.com", WorkspaceRole.Member, InvitationTokenHash.Create("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"), 1, Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.Expire(DateTimeOffset.UtcNow);

        Action act = () => invitation.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation is not pending.");
    }

    [CoversMutation(typeof(WorkspaceInvitation), "Resend(Notrelix.Domain.Workspaces.Invitations.InvitationTokenHash,System.Int32,System.DateTimeOffset,System.TimeSpan,System.Guid)", MutationScenario.Event)]
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

    [CoversMutation(typeof(WorkspaceInvitation), "Decline(System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
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

    [CoversMutation(typeof(WorkspaceInvitation), "Decline(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
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

    [CoversMutation(typeof(WorkspaceInvitation), "ChangeRole(Notrelix.Domain.Workspaces.Members.WorkspaceRole,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
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

    [CoversMutation(typeof(WorkspaceInvitation), "ChangeRole(Notrelix.Domain.Workspaces.Members.WorkspaceRole,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
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

    [CoversMutation(typeof(WorkspaceInvitation), "ChangeRole(Notrelix.Domain.Workspaces.Members.WorkspaceRole,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
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

    [CoversMutation(typeof(WorkspaceInvitation), "Accept(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Accept_Expired_ShouldNotMutateStatus()
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
            now,
            TimeSpan.FromDays(1));
        ((IHasDomainEvents)invitation).ClearDomainEvents();
        var originalStatus = invitation.Status;

        var act = () => invitation.Accept(Guid.NewGuid(), now.AddDays(2));

        act.Should().Throw<BusinessRuleException>();
        invitation.Status.Should().Be(originalStatus);
        invitation.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(WorkspaceInvitation), "Accept(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Accept_NotPending_ShouldNotMutateStatus()
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
        ((IHasDomainEvents)invitation).ClearDomainEvents();
        var originalStatus = invitation.Status;

        var act = () => invitation.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        invitation.Status.Should().Be(originalStatus);
        invitation.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(WorkspaceInvitation), "Decline(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Decline_NotPending_ShouldNotMutateStatus()
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
        ((IHasDomainEvents)invitation).ClearDomainEvents();
        var originalStatus = invitation.Status;

        var act = () => invitation.Decline(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        invitation.Status.Should().Be(originalStatus);
        invitation.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(WorkspaceInvitation), "ChangeRole(Notrelix.Domain.Workspaces.Members.WorkspaceRole,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void ChangeRole_NotPending_ShouldNotMutateRole()
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
        ((IHasDomainEvents)invitation).ClearDomainEvents();
        var originalRole = invitation.Role;

        var act = () => invitation.ChangeRole(WorkspaceRole.Admin, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        invitation.Role.Should().Be(originalRole);
        invitation.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(WorkspaceInvitation), "ChangeRole(Notrelix.Domain.Workspaces.Members.WorkspaceRole,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void ChangeRole_OwnerRole_ShouldNotMutateRole()
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
        var originalRole = invitation.Role;

        var act = () => invitation.ChangeRole(WorkspaceRole.Owner, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        invitation.Role.Should().Be(originalRole);
        invitation.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(WorkspaceInvitation), "Accept(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Accept_EmptyActor_ShouldNotMutateStatus()
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
        var originalStatus = invitation.Status;

        var act = () => invitation.Accept(Guid.Empty, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        invitation.Status.Should().Be(originalStatus);
        invitation.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(WorkspaceInvitation), "Decline(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void Decline_EmptyActor_ShouldNotMutateStatus()
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
        var originalStatus = invitation.Status;

        var act = () => invitation.Decline(Guid.Empty, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        invitation.Status.Should().Be(originalStatus);
        invitation.DomainEvents.Should().BeEmpty();
    }
}
