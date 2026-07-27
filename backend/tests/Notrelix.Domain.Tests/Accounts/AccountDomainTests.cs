using FluentAssertions;
using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.Domains.Events;

namespace Notrelix.Domain.Tests.Accounts;

public class AccountDomainTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var domain = AccountDomain.Create(_accountId, "Example.COM", _actorId, _now, "token-hash");

        domain.AccountId.Should().Be(_accountId);
        domain.Domain.Should().Be("example.com");
        domain.VerificationStatus.Should().Be(DomainVerificationStatus.Pending);
        domain.VerificationTokenHash.Should().Be("token-hash");
        domain.AutoJoinEnabled.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldSetCreationAudit()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);

        domain.CreatedBy.Should().Be(_actorId);
        domain.CreatedAt.Should().Be(_now);
    }

    [Fact]
    public void Create_ShouldRaiseCreationEvent()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);

        domain.DomainEvents.Should().ContainSingle(e => e is AccountDomainCreatedDomainEvent);
        var evt = (AccountDomainCreatedDomainEvent)domain.DomainEvents.First(e => e is AccountDomainCreatedDomainEvent);
        evt.AccountId.Should().Be(_accountId);
        evt.DomainId.Should().Be(domain.Id);
        evt.Domain.Should().Be("example.com");
        evt.CreatedBy.Should().Be(_actorId);
    }

    [Fact]
    public void Create_WithEmptyActorId_ShouldThrow()
    {
        var act = () => AccountDomain.Create(_accountId, "example.com", Guid.Empty, _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => AccountDomain.Create(Guid.Empty, "example.com", _actorId, _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyDomain_ShouldThrow()
    {
        var act = () => AccountDomain.Create(_accountId, "  ", _actorId, _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Verify_ShouldChangeStatusToVerified_AndRaiseEvent()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);

        domain.Verify(_now, _actorId);

        domain.VerificationStatus.Should().Be(DomainVerificationStatus.Verified);
        domain.VerifiedAt.Should().Be(_now);
        domain.DomainEvents.Should().ContainSingle(e => e is AccountDomainVerifiedDomainEvent);
    }

    [Fact]
    public void Verify_WhenAlreadyVerified_ShouldBeIdempotent()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);
        domain.Verify(_now, _actorId);
        ((IHasDomainEvents)domain).ClearDomainEvents();

        domain.Verify(_now.AddHours(1), _actorId);

        domain.VerificationStatus.Should().Be(DomainVerificationStatus.Verified);
        domain.VerifiedAt.Should().Be(_now);
        domain.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Reject_ShouldChangeStatusToRejected_AndRaiseEvent()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);

        domain.Reject(_actorId, _now);

        domain.VerificationStatus.Should().Be(DomainVerificationStatus.Rejected);
        domain.DomainEvents.Should().ContainSingle(e => e is AccountDomainRejectedDomainEvent);
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ShouldBeIdempotent()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);
        domain.Reject(_actorId, _now);
        ((IHasDomainEvents)domain).ClearDomainEvents();

        domain.Reject(_actorId, _now);

        domain.VerificationStatus.Should().Be(DomainVerificationStatus.Rejected);
        domain.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void EnableAutoJoin_WhenNotVerified_ShouldThrow()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);

        var act = () => domain.EnableAutoJoin(_actorId, _now);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*unverified*");
    }

    [Fact]
    public void EnableAutoJoin_WhenRejected_ShouldThrow()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);
        domain.Reject(_actorId, _now);

        var act = () => domain.EnableAutoJoin(_actorId, _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void EnableAutoJoin_WhenVerified_ShouldSucceed_AndRaiseEvent()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);
        domain.Verify(_now, _actorId);
        ((IHasDomainEvents)domain).ClearDomainEvents();

        domain.EnableAutoJoin(_actorId, _now);

        domain.AutoJoinEnabled.Should().BeTrue();
        domain.DomainEvents.Should().ContainSingle(e => e is AccountDomainAutoJoinEnabledDomainEvent);
    }

    [Fact]
    public void EnableAutoJoin_WhenAlreadyEnabled_ShouldBeIdempotent()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);
        domain.Verify(_now, _actorId);
        domain.EnableAutoJoin(_actorId, _now);
        ((IHasDomainEvents)domain).ClearDomainEvents();

        domain.EnableAutoJoin(_actorId, _now);

        domain.AutoJoinEnabled.Should().BeTrue();
        domain.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DisableAutoJoin_ShouldSetToFalse_AndRaiseEvent()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);
        domain.Verify(_now, _actorId);
        domain.EnableAutoJoin(_actorId, _now);
        ((IHasDomainEvents)domain).ClearDomainEvents();

        domain.DisableAutoJoin(_actorId, _now);

        domain.AutoJoinEnabled.Should().BeFalse();
        domain.DomainEvents.Should().ContainSingle(e => e is AccountDomainAutoJoinDisabledDomainEvent);
    }

    [Fact]
    public void DisableAutoJoin_WhenAlreadyDisabled_ShouldBeIdempotent()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);
        ((IHasDomainEvents)domain).ClearDomainEvents();

        domain.DisableAutoJoin(_actorId, _now);

        domain.AutoJoinEnabled.Should().BeFalse();
        domain.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Verify_ShouldSetAuditOnUpdate()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);

        domain.Verify(_now, _actorId);

        domain.UpdatedAt.Should().Be(_now);
        domain.UpdatedBy.Should().Be(_actorId);
    }

    [Fact]
    public void Verify_ShouldIncrementVersion()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);
        var versionBefore = domain.Version;

        domain.Verify(_now, _actorId);

        domain.Version.Should().Be(versionBefore + 1);
    }
}
