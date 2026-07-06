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
        invitation.ClearDomainEvents();

        invitation.Accept(Guid.NewGuid(), _now);

        invitation.Status.Should().Be(AccountInvitationStatus.Accepted);
        invitation.DomainEvents.Should().ContainSingle(e => e is AccountInvitationAcceptedDomainEvent);
    }

    [Fact]
    public void Accept_ExpiredInvitation_ShouldThrow()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now, TimeSpan.FromDays(1));
        invitation.ClearDomainEvents();

        var act = () => invitation.Accept(Guid.NewGuid(), _now.AddDays(2));
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation has expired.");
    }

    [Fact]
    public void Accept_AlreadyAccepted_ShouldThrow()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now);
        invitation.Accept(Guid.NewGuid(), _now);
        invitation.ClearDomainEvents();

        var act = () => invitation.Accept(Guid.NewGuid(), _now);
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation is not pending.");
    }

    [Fact]
    public void Expire_ShouldSucceed()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now);
        invitation.ClearDomainEvents();

        invitation.Expire(_now.AddDays(8));

        invitation.Status.Should().Be(AccountInvitationStatus.Expired);
        invitation.DomainEvents.Should().ContainSingle(e => e is AccountInvitationExpiredDomainEvent);
    }

    [Fact]
    public void Expire_AlreadyAccepted_ShouldNotChange()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now);
        invitation.Accept(Guid.NewGuid(), _now);
        invitation.ClearDomainEvents();

        invitation.Expire(_now.AddDays(8));

        invitation.DomainEvents.Should().BeEmpty();
        invitation.Status.Should().Be(AccountInvitationStatus.Accepted);
    }

    [Fact]
    public void Revoke_ShouldSucceed()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now);
        invitation.ClearDomainEvents();

        invitation.Revoke(_invitedBy, _now);

        invitation.Status.Should().Be(AccountInvitationStatus.Revoked);
        invitation.DomainEvents.Should().ContainSingle(e => e is AccountInvitationRevokedDomainEvent);
    }

    [Fact]
    public void Revoke_ExpiredInvitation_ShouldThrow()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now, TimeSpan.FromDays(1));
        invitation.ClearDomainEvents();

        var act = () => invitation.Revoke(_invitedBy, _now.AddDays(2));
        act.Should().Throw<BusinessRuleException>().WithMessage("Invitation has expired.");
    }

    [Fact]
    public void Revoke_AlreadyAccepted_ShouldNotChange()
    {
        var invitation = AccountInvitation.Create(_accountId, "user@example.com", AccountRole.Member, _invitedBy, _now);
        invitation.Accept(Guid.NewGuid(), _now);
        invitation.ClearDomainEvents();

        invitation.Revoke(_invitedBy, _now);

        invitation.DomainEvents.Should().BeEmpty();
        invitation.Status.Should().Be(AccountInvitationStatus.Accepted);
    }
}
