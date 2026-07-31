using FluentAssertions;
using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.Domains.Events;
using Notrelix.Domain.Tests.Freeze;

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

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.Verify), MutationScenario.Event, typeof(DateTimeOffset), typeof(Guid))]
    [Fact]
    public void Verify_ShouldChangeStatusToVerified_AndRaiseEvent()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);

        domain.Verify(_now, _actorId);

        domain.VerificationStatus.Should().Be(DomainVerificationStatus.Verified);
        domain.VerifiedAt.Should().Be(_now);
        domain.DomainEvents.Should().ContainSingle(e => e is AccountDomainVerifiedDomainEvent);
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.Verify), MutationScenario.NoOp, typeof(DateTimeOffset), typeof(Guid))]
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

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.Reject), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Reject_ShouldChangeStatusToRejected_AndRaiseEvent()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);

        domain.Reject(_actorId, _now);

        domain.VerificationStatus.Should().Be(DomainVerificationStatus.Rejected);
        domain.DomainEvents.Should().ContainSingle(e => e is AccountDomainRejectedDomainEvent);
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.Reject), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.EnableAutoJoin), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void EnableAutoJoin_WhenNotVerified_ShouldThrow()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);

        var act = () => domain.EnableAutoJoin(_actorId, _now);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*unverified*");
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.Reject), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void EnableAutoJoin_WhenRejected_ShouldThrow()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);
        domain.Reject(_actorId, _now);

        var act = () => domain.EnableAutoJoin(_actorId, _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.EnableAutoJoin), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.EnableAutoJoin), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.DisableAutoJoin), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.DisableAutoJoin), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void DisableAutoJoin_WhenAlreadyDisabled_ShouldBeIdempotent()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);
        ((IHasDomainEvents)domain).ClearDomainEvents();

        domain.DisableAutoJoin(_actorId, _now);

        domain.AutoJoinEnabled.Should().BeFalse();
        domain.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.Verify), MutationScenario.Audit, typeof(DateTimeOffset), typeof(Guid))]
    [Fact]
    public void Verify_ShouldSetAuditOnUpdate()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);

        domain.Verify(_now, _actorId);

        domain.UpdatedAt.Should().Be(_now);
        domain.UpdatedBy.Should().Be(_actorId);
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.Verify), MutationScenario.Version, typeof(DateTimeOffset), typeof(Guid))]
    [Fact]
    public void Verify_ShouldIncrementVersion()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);
        var versionBefore = domain.Version;

        domain.Verify(_now, _actorId);

        domain.Version.Should().Be(versionBefore + 1);
    }

    private AccountDomain CreateDomain()
    {
        return AccountDomain.Create(_accountId, "example.com", _actorId, _now);
    }

    private AccountDomain CreateVerifiedDomain()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", _actorId, _now);
        domain.Verify(_now, _actorId);
        ((IHasDomainEvents)domain).ClearDomainEvents();
        return domain;
    }

    [Fact]
    public void InitialVersion_ShouldBe1()
    {
        var domain = AccountDomain.Create(_accountId, "example.com", Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        domain.Version.Should().Be(1);
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.Reject), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Reject_ShouldIncrementVersion()
    {
        var domain = CreateDomain();
        var before = domain.Version;
        domain.Reject(Guid.NewGuid(), DateTimeOffset.UtcNow);
        domain.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.Reject), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Reject_ShouldSetAudit()
    {
        var domain = CreateDomain();
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        domain.Reject(actor, time);
        domain.UpdatedBy.Should().Be(actor);
        domain.UpdatedAt.Should().Be(time);
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.EnableAutoJoin), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void EnableAutoJoin_ShouldIncrementVersion()
    {
        var domain = CreateVerifiedDomain();
        var before = domain.Version;
        domain.EnableAutoJoin(Guid.NewGuid(), DateTimeOffset.UtcNow);
        domain.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.EnableAutoJoin), MutationScenario.Audit, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void EnableAutoJoin_ShouldSetAudit()
    {
        var domain = CreateVerifiedDomain();
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        domain.EnableAutoJoin(actor, time);
        domain.UpdatedBy.Should().Be(actor);
        domain.UpdatedAt.Should().Be(time);
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.DisableAutoJoin), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void DisableAutoJoin_ShouldIncrementVersion()
    {
        var domain = CreateVerifiedDomain();
        domain.EnableAutoJoin(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var before = domain.Version;
        domain.DisableAutoJoin(Guid.NewGuid(), DateTimeOffset.UtcNow);
        domain.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.DisableAutoJoin), MutationScenario.Audit, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void DisableAutoJoin_ShouldSetAudit()
    {
        var domain = CreateVerifiedDomain();
        domain.EnableAutoJoin(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        domain.DisableAutoJoin(actor, time);
        domain.UpdatedBy.Should().Be(actor);
        domain.UpdatedAt.Should().Be(time);
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.Verify), MutationScenario.NoOp, typeof(DateTimeOffset), typeof(Guid))]
    [Fact]
    public void Verify_NoOp_VersionShouldNotIncrement()
    {
        var domain = CreateVerifiedDomain();
        var before = domain.Version;
        domain.Verify(DateTimeOffset.UtcNow, Guid.NewGuid());
        domain.Version.Should().Be(before);
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.Reject), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Reject_NoOp_VersionShouldNotIncrement()
    {
        var domain = CreateDomain();
        domain.Reject(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var before = domain.Version;
        domain.Reject(Guid.NewGuid(), DateTimeOffset.UtcNow);
        domain.Version.Should().Be(before);
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.EnableAutoJoin), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void EnableAutoJoin_NoOp_VersionShouldNotIncrement()
    {
        var domain = CreateVerifiedDomain();
        domain.EnableAutoJoin(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var before = domain.Version;
        domain.EnableAutoJoin(Guid.NewGuid(), DateTimeOffset.UtcNow);
        domain.Version.Should().Be(before);
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.DisableAutoJoin), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void DisableAutoJoin_NoOp_VersionShouldNotIncrement()
    {
        var domain = CreateVerifiedDomain();
        var before = domain.Version;
        domain.DisableAutoJoin(Guid.NewGuid(), DateTimeOffset.UtcNow);
        domain.Version.Should().Be(before);
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.Verify), MutationScenario.Event, typeof(DateTimeOffset), typeof(Guid))]
    [Fact]
    public void Verify_ShouldRaiseEvent_WithCorrectPayload()
    {
        var domain = CreateDomain();
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        domain.Verify(time, actor);
        var evt = domain.DomainEvents.OfType<DomainEvent>().Last();
        evt.Should().BeOfType<AccountDomainVerifiedDomainEvent>();
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.Reject), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Reject_ShouldRaiseEvent_WithCorrectPayload()
    {
        var domain = CreateDomain();
        var actor = Guid.NewGuid();
        domain.Reject(actor, DateTimeOffset.UtcNow);
        var evt = domain.DomainEvents.OfType<DomainEvent>().Last();
        evt.Should().BeOfType<AccountDomainRejectedDomainEvent>();
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.EnableAutoJoin), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void EnableAutoJoin_ShouldRaiseEvent_WithCorrectPayload()
    {
        var domain = CreateVerifiedDomain();
        var actor = Guid.NewGuid();
        domain.EnableAutoJoin(actor, DateTimeOffset.UtcNow);
        var evt = domain.DomainEvents.OfType<DomainEvent>().Last();
        evt.Should().BeOfType<AccountDomainAutoJoinEnabledDomainEvent>();
    }

    [CoversMutation(typeof(AccountDomain), nameof(AccountDomain.DisableAutoJoin), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void DisableAutoJoin_ShouldRaiseEvent_WithCorrectPayload()
    {
        var domain = CreateVerifiedDomain();
        domain.EnableAutoJoin(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var actor = Guid.NewGuid();
        domain.DisableAutoJoin(actor, DateTimeOffset.UtcNow);
        var evt = domain.DomainEvents.OfType<DomainEvent>().Last();
        evt.Should().BeOfType<AccountDomainAutoJoinDisabledDomainEvent>();
    }
}
