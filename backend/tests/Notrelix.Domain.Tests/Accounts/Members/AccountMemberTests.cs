using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Accounts;

[CoversAggregate(typeof(AccountMember))]
public class AccountMemberTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSucceed()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Member, _actorId, _now);

        member.AccountId.Should().Be(_accountId);
        member.UserId.Should().Be(_userId);
        member.Role.Should().Be(AccountRole.Member);
        member.Status.Should().Be(AccountMemberStatus.Active);
        member.DomainEvents.Should().ContainSingle(e => e is AccountMemberAddedDomainEvent);
    }

    [Fact]
    public void Create_Owner_ShouldSucceed()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Owner, _actorId, _now);

        member.Role.Should().Be(AccountRole.Owner);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.ChangeRole), MutationScenario.Valid, typeof(AccountRole), typeof(Guid), typeof(int), typeof(DateTimeOffset))]
    [Fact]
    public void ChangeRole_ShouldSucceed()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Member, _actorId, _now);
        ((IHasDomainEvents)member).ClearDomainEvents();

        member.ChangeRole(AccountRole.Admin, _actorId, 2, _now);

        member.Role.Should().Be(AccountRole.Admin);
        member.DomainEvents.Should().ContainSingle(e => e is AccountMemberRoleChangedDomainEvent);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.ChangeRole), MutationScenario.Invalid, typeof(AccountRole), typeof(Guid), typeof(int), typeof(DateTimeOffset))]
    [Fact]
    public void ChangeRole_OwnerToMember_WithSingleOwner_ShouldThrow()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Owner, _actorId, _now);
        ((IHasDomainEvents)member).ClearDomainEvents();

        var act = () => member.ChangeRole(AccountRole.Member, _actorId, 1, _now);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot downgrade the last owner of the account.");
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.ChangeRole), MutationScenario.Valid, typeof(AccountRole), typeof(Guid), typeof(int), typeof(DateTimeOffset))]
    [Fact]
    public void ChangeRole_OwnerToMember_WithMultipleOwners_ShouldSucceed()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Owner, _actorId, _now);
        ((IHasDomainEvents)member).ClearDomainEvents();

        member.ChangeRole(AccountRole.Member, _actorId, 2, _now);

        member.Role.Should().Be(AccountRole.Member);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Suspend), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset), typeof(int))]
    [Fact]
    public void Suspend_ShouldSucceed()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Member, _actorId, _now);
        ((IHasDomainEvents)member).ClearDomainEvents();

        member.Suspend(_actorId, _now, 2);

        member.Status.Should().Be(AccountMemberStatus.Suspended);
        member.DomainEvents.Should().ContainSingle(e => e is AccountMemberSuspendedDomainEvent);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Suspend), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset), typeof(int))]
    [Fact]
    public void Suspend_LastOwner_ShouldThrow()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Owner, _actorId, _now);
        ((IHasDomainEvents)member).ClearDomainEvents();

        var act = () => member.Suspend(_actorId, _now, 1);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot suspend the last owner of the account.");
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Activate), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Activate_ShouldSucceed()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Member, _actorId, _now);
        member.Suspend(_actorId, _now, 2);
        ((IHasDomainEvents)member).ClearDomainEvents();

        member.Activate(_actorId, _now);

        member.Status.Should().Be(AccountMemberStatus.Active);
        member.DomainEvents.Should().ContainSingle(e => e is AccountMemberActivatedDomainEvent);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Remove), MutationScenario.Invalid, typeof(int), typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Activate_RemovedMember_ShouldThrow()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Member, _actorId, _now);
        member.Remove(2, _actorId, _now);
        ((IHasDomainEvents)member).ClearDomainEvents();

        var act = () => member.Activate(_actorId, _now);
        var exception = act.Should().Throw<DomainException>().Which;
        exception.Message.Should().Contain("has been deleted");
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Remove), MutationScenario.Valid, typeof(int), typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Remove_ShouldSucceed()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Member, _actorId, _now);
        ((IHasDomainEvents)member).ClearDomainEvents();

        member.Remove(2, _actorId, _now);

        member.IsDeleted.Should().BeTrue();
        member.DomainEvents.Should().ContainSingle(e => e is AccountMemberRemovedDomainEvent);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Remove), MutationScenario.Invalid, typeof(int), typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Remove_LastOwner_ShouldThrow()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Owner, _actorId, _now);
        ((IHasDomainEvents)member).ClearDomainEvents();

        var act = () => member.Remove(1, _actorId, _now);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot remove the last owner of the account.");
    }

    private AccountMember CreateMember(AccountRole role = AccountRole.Member)
    {
        return AccountMember.Create(_accountId, _userId, role, _actorId, _now);
    }

    [Fact]
    public void InitialVersion_ShouldBe1()
    {
        var member = CreateMember();
        member.Version.Should().Be(1);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.ChangeRole), MutationScenario.Version, typeof(AccountRole), typeof(Guid), typeof(int), typeof(DateTimeOffset))]
    [Fact]
    public void ChangeRole_ShouldIncrementVersion()
    {
        var member = CreateMember();
        var before = member.Version;
        member.ChangeRole(AccountRole.Admin, Guid.NewGuid(), 2, DateTimeOffset.UtcNow);
        member.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.ChangeRole), MutationScenario.Audit, typeof(AccountRole), typeof(Guid), typeof(int), typeof(DateTimeOffset))]
    [Fact]
    public void ChangeRole_ShouldSetAudit()
    {
        var member = CreateMember();
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        member.ChangeRole(AccountRole.Admin, actor, 2, time);
        member.UpdatedBy.Should().Be(actor);
        member.UpdatedAt.Should().Be(time);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.ChangeRole), MutationScenario.Event, typeof(AccountRole), typeof(Guid), typeof(int), typeof(DateTimeOffset))]
    [Fact]
    public void ChangeRole_ToSameRole_ShouldNotRaiseEvent()
    {
        var member = CreateMember(AccountRole.Member);
        ((IHasDomainEvents)member).ClearDomainEvents();
        member.ChangeRole(AccountRole.Member, Guid.NewGuid(), 2, DateTimeOffset.UtcNow);
        member.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.ChangeRole), MutationScenario.Version, typeof(AccountRole), typeof(Guid), typeof(int), typeof(DateTimeOffset))]
    [Fact]
    public void ChangeRole_ToSameRole_ShouldNotIncrementVersion()
    {
        var member = CreateMember(AccountRole.Member);
        var before = member.Version;
        member.ChangeRole(AccountRole.Member, Guid.NewGuid(), 2, DateTimeOffset.UtcNow);
        member.Version.Should().Be(before);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Suspend), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset), typeof(int))]
    [Fact]
    public void Suspend_ShouldIncrementVersion()
    {
        var member = CreateMember();
        var before = member.Version;
        member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow, 2);
        member.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Suspend), MutationScenario.Audit, typeof(Guid), typeof(DateTimeOffset), typeof(int))]
    [Fact]
    public void Suspend_ShouldSetAudit()
    {
        var member = CreateMember();
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        member.Suspend(actor, time, 2);
        member.UpdatedBy.Should().Be(actor);
        member.UpdatedAt.Should().Be(time);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Suspend), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset), typeof(int))]
    [Fact]
    public void Suspend_WhenAlreadySuspended_ShouldNotRaiseEvent()
    {
        var member = CreateMember();
        member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow, 2);
        ((IHasDomainEvents)member).ClearDomainEvents();
        member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow, 2);
        member.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Suspend), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset), typeof(int))]
    [Fact]
    public void Suspend_WhenAlreadySuspended_ShouldNotIncrementVersion()
    {
        var member = CreateMember();
        member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow, 2);
        var before = member.Version;
        member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow, 2);
        member.Version.Should().Be(before);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Activate), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Activate_ShouldIncrementVersion()
    {
        var member = CreateMember();
        member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow, 2);
        var before = member.Version;
        member.Activate(Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Activate), MutationScenario.Audit, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Activate_ShouldSetAudit()
    {
        var member = CreateMember();
        member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow, 2);
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        member.Activate(actor, time);
        member.UpdatedBy.Should().Be(actor);
        member.UpdatedAt.Should().Be(time);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Activate), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Activate_WhenAlreadyActive_ShouldNotRaiseEvent()
    {
        var member = CreateMember();
        ((IHasDomainEvents)member).ClearDomainEvents();
        member.Activate(Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Activate), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Activate_WhenAlreadyActive_ShouldNotIncrementVersion()
    {
        var member = CreateMember();
        var before = member.Version;
        member.Activate(Guid.NewGuid(), DateTimeOffset.UtcNow);
        member.Version.Should().Be(before);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Remove), MutationScenario.Version, typeof(int), typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Remove_ShouldIncrementVersion()
    {
        var member = CreateMember();
        var before = member.Version;
        member.Remove(2, Guid.NewGuid(), DateTimeOffset.UtcNow, "reason");
        member.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Remove), MutationScenario.Lifecycle, typeof(int), typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Remove_ShouldSetDeleteAudit()
    {
        var member = CreateMember();
        var actor = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        member.Remove(2, actor, time, "reason");
        member.DeletedBy.Should().Be(actor);
        member.DeletedAt.Should().Be(time);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Remove), MutationScenario.Lifecycle, typeof(int), typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Remove_ShouldRaiseEvent()
    {
        var member = CreateMember();
        member.Remove(2, Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        member.DomainEvents.Should().ContainSingle(e => e is AccountMemberRemovedDomainEvent);
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Remove), MutationScenario.Invalid, typeof(int), typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void ChangeRole_AfterRemove_ShouldThrow()
    {
        var member = CreateMember();
        member.Remove(2, Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        var act = () => member.ChangeRole(AccountRole.Admin, Guid.NewGuid(), 2, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(AccountMember), nameof(AccountMember.Remove), MutationScenario.Invalid, typeof(int), typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Suspend_AfterRemove_ShouldThrow()
    {
        var member = CreateMember();
        member.Remove(2, Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        var act = () => member.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow, 2);
        act.Should().Throw<BusinessRuleException>();
    }
}
