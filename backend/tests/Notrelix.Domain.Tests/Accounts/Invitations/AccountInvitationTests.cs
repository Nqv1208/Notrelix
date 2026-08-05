using FluentAssertions;

namespace Notrelix.Domain.Tests.Accounts;

public class AccountInvitationTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _invitedBy = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSucceed()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now);

        invitation.Email.Should().Be("user@example.com");
        invitation.Role.Should().Be(AccountRole.Member);
        invitation.Status.Should().Be(AccountInvitationStatus.Pending);
        invitation.ExpiresAt.Should().BeCloseTo(_now.AddDays(7), TimeSpan.FromSeconds(1));
        invitation.DomainEvents.Should().ContainSingle(e => e is AccountInvitationCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithCustomExpiry_ShouldUseCustomExpiry()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now, TimeSpan.FromDays(1));

        invitation.ExpiresAt.Should().BeCloseTo(_now.AddDays(1), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_ZeroExpiry_ShouldThrow()
    {
        var act = () => AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now, TimeSpan.Zero);
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation expiry must be greater than zero.");
    }

    [Fact]
    public void Create_EmptyEmail_ShouldThrow()
    {
        var act = () => AccountInvitation.Create(_accountId, "", AccountRole.Member, _invitedBy, _now);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Accept_ShouldSucceed()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now);
        ((IHasDomainEvents)invitation).ClearDomainEvents();

        invitation.Accept(Guid.NewGuid(), _now);

        invitation.Status.Should().Be(AccountInvitationStatus.Accepted);
        invitation.DomainEvents.Should().ContainSingle(e => e is AccountInvitationAcceptedDomainEvent);
    }

    [Fact]
    public void Accept_ExpiredInvitation_ShouldThrow()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now, TimeSpan.FromDays(1));
        ((IHasDomainEvents)invitation).ClearDomainEvents();

        var act = () => invitation.Accept(Guid.NewGuid(), _now.AddDays(2));
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation has expired.");
    }

    [Fact]
    public void Accept_AlreadyAccepted_ShouldThrow()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now);
        invitation.Accept(Guid.NewGuid(), _now);
        ((IHasDomainEvents)invitation).ClearDomainEvents();

        var act = () => invitation.Accept(Guid.NewGuid(), _now);
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation is not pending.");
    }

    [Fact]
    public void Expire_ShouldSucceed()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now);
        ((IHasDomainEvents)invitation).ClearDomainEvents();

        invitation.Expire(_now.AddDays(8));

        invitation.Status.Should().Be(AccountInvitationStatus.Expired);
        invitation.DomainEvents.Should().ContainSingle(e => e is AccountInvitationExpiredDomainEvent);
    }

    [Fact]
    public void Expire_AlreadyAccepted_ShouldNotChange()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now);
        invitation.Accept(Guid.NewGuid(), _now);
        ((IHasDomainEvents)invitation).ClearDomainEvents();

        invitation.Expire(_now.AddDays(8));

        invitation.DomainEvents.Should().BeEmpty();
        invitation.Status.Should().Be(AccountInvitationStatus.Accepted);
    }

    [Fact]
    public void Revoke_ShouldSucceed()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now);
        ((IHasDomainEvents)invitation).ClearDomainEvents();

        invitation.Revoke(_invitedBy, _now);

        invitation.Status.Should().Be(AccountInvitationStatus.Revoked);
        invitation.DomainEvents.Should().ContainSingle(e => e is AccountInvitationRevokedDomainEvent);
    }

    [Fact]
    public void Revoke_ExpiredInvitation_ShouldThrow()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now, TimeSpan.FromDays(1));
        ((IHasDomainEvents)invitation).ClearDomainEvents();

        var act = () => invitation.Revoke(_invitedBy, _now.AddDays(2));
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation has expired.");
    }

    [Fact]
    public void Revoke_AlreadyAccepted_ShouldNotChange()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now);
        invitation.Accept(Guid.NewGuid(), _now);
        ((IHasDomainEvents)invitation).ClearDomainEvents();

        invitation.Revoke(_invitedBy, _now);

        invitation.DomainEvents.Should().BeEmpty();
        invitation.Status.Should().Be(AccountInvitationStatus.Accepted);
    }

    private AccountInvitation CreateInvitation()
    {
        return AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now);
    }

    [Fact]
    public void InitialVersion_ShouldBe1()
    {
        var invitation = CreateInvitation();
        invitation.Version.Should().Be(1);
    }

    [Fact]
    public void Accept_ShouldIncrementVersion()
    {
        var invitation = CreateInvitation();
        var before = invitation.Version;
        invitation.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.Version.Should().Be(before + 1);
    }

    [Fact]
    public void Accept_ShouldSetAudit()
    {
        var invitation = CreateInvitation();
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        invitation.Accept(actor, time);
        invitation.UpdatedBy.Should().Be(actor);
        invitation.UpdatedAt.Should().Be(time);
    }

    [Fact]
    public void Expire_ShouldIncrementVersion()
    {
        var invitation = CreateInvitation();
        var before = invitation.Version;
        invitation.Expire(DateTimeOffset.UtcNow);
        invitation.Version.Should().Be(before + 1);
    }

    [Fact]
    public void Expire_ShouldSetAudit()
    {
        var invitation = CreateInvitation();
        var time = DateTimeOffset.UtcNow;
        invitation.Expire(time);
        invitation.UpdatedAt.Should().Be(time);
    }

    [Fact]
    public void Revoke_ShouldIncrementVersion()
    {
        var invitation = CreateInvitation();
        var before = invitation.Version;
        invitation.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.Version.Should().Be(before + 1);
    }

    [Fact]
    public void Revoke_ShouldSetAudit()
    {
        var invitation = CreateInvitation();
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        invitation.Revoke(actor, time);
        invitation.UpdatedBy.Should().Be(actor);
        invitation.UpdatedAt.Should().Be(time);
    }

    [Fact]
    public void Accept_ShouldRaiseEvent_WithCorrectPayload()
    {
        var invitation = CreateInvitation();
        var userId = Guid.NewGuid();
        invitation.Accept(userId, DateTimeOffset.UtcNow);
        var evt = invitation.DomainEvents.OfType<DomainEvent>().Last();
        evt.Should().BeOfType<AccountInvitationAcceptedDomainEvent>();
    }

    [Fact]
    public void Expire_ShouldRaiseEvent_WithCorrectPayload()
    {
        var invitation = CreateInvitation();
        invitation.Expire(DateTimeOffset.UtcNow);
        var evt = invitation.DomainEvents.OfType<DomainEvent>().Last();
        evt.Should().BeOfType<AccountInvitationExpiredDomainEvent>();
    }

    [Fact]
    public void Revoke_ShouldRaiseEvent_WithCorrectPayload()
    {
        var invitation = CreateInvitation();
        var actor = Guid.NewGuid();
        invitation.Revoke(actor, DateTimeOffset.UtcNow);
        var evt = invitation.DomainEvents.OfType<DomainEvent>().Last();
        evt.Should().BeOfType<AccountInvitationRevokedDomainEvent>();
    }

    [Fact]
    public void Expire_WhenAlreadyExpired_ShouldNotChange()
    {
        var invitation = CreateInvitation();
        invitation.Expire(DateTimeOffset.UtcNow);
        var before = invitation.Version;
        invitation.Expire(DateTimeOffset.UtcNow);
        invitation.Version.Should().Be(before);
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ShouldNotChange()
    {
        var invitation = CreateInvitation();
        invitation.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var before = invitation.Version;
        invitation.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);
        invitation.Version.Should().Be(before);
    }

    [Fact]
    public void Accept_RevokedInvitation_ShouldThrow()
    {
        var invitation = CreateInvitation();
        invitation.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var act = () => invitation.Accept(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }
}
