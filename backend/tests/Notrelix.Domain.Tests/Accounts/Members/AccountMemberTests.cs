using FluentAssertions;

namespace Notrelix.Domain.Tests.Accounts;

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

    [Fact]
    public void ChangeRole_ShouldSucceed()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Member, _actorId, _now);
        member.ClearDomainEvents();

        member.ChangeRole(AccountRole.Admin, _actorId, 2, _now);

        member.Role.Should().Be(AccountRole.Admin);
        member.DomainEvents.Should().ContainSingle(e => e is AccountMemberRoleChangedDomainEvent);
    }

    [Fact]
    public void ChangeRole_OwnerToMember_WithSingleOwner_ShouldThrow()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Owner, _actorId, _now);
        member.ClearDomainEvents();

        var act = () => member.ChangeRole(AccountRole.Member, _actorId, 1, _now);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot downgrade the last owner of the account.");
    }

    [Fact]
    public void ChangeRole_OwnerToMember_WithMultipleOwners_ShouldSucceed()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Owner, _actorId, _now);
        member.ClearDomainEvents();

        member.ChangeRole(AccountRole.Member, _actorId, 2, _now);

        member.Role.Should().Be(AccountRole.Member);
    }

    [Fact]
    public void Suspend_ShouldSucceed()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Member, _actorId, _now);
        member.ClearDomainEvents();

        member.Suspend(_actorId, _now, 2);

        member.Status.Should().Be(AccountMemberStatus.Suspended);
        member.DomainEvents.Should().ContainSingle(e => e is AccountMemberSuspendedDomainEvent);
    }

    [Fact]
    public void Suspend_LastOwner_ShouldThrow()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Owner, _actorId, _now);
        member.ClearDomainEvents();

        var act = () => member.Suspend(_actorId, _now, 1);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot suspend the last owner of the account.");
    }

    [Fact]
    public void Activate_ShouldSucceed()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Member, _actorId, _now);
        member.Suspend(_actorId, _now, 2);
        member.ClearDomainEvents();

        member.Activate(_actorId, _now);

        member.Status.Should().Be(AccountMemberStatus.Active);
        member.DomainEvents.Should().ContainSingle(e => e is AccountMemberActivatedDomainEvent);
    }

    [Fact]
    public void Activate_RemovedMember_ShouldThrow()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Member, _actorId, _now);
        member.Remove(2, _actorId, _now);
        member.ClearDomainEvents();

        var act = () => member.Activate(_actorId, _now);
        var exception = act.Should().Throw<DomainException>().Which;
        exception.Message.Should().Contain("has been deleted");
    }

    [Fact]
    public void Remove_ShouldSucceed()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Member, _actorId, _now);
        member.ClearDomainEvents();

        member.Remove(2, _actorId, _now);

        member.IsDeleted.Should().BeTrue();
        member.Status.Should().Be(AccountMemberStatus.Removed);
        member.DomainEvents.Should().ContainSingle(e => e is AccountMemberRemovedDomainEvent);
    }

    [Fact]
    public void Remove_LastOwner_ShouldThrow()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Owner, _actorId, _now);
        member.ClearDomainEvents();

        var act = () => member.Remove(1, _actorId, _now);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot remove the last owner of the account.");
    }

    [Fact]
    public void Restore_ShouldSucceed()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Member, _actorId, _now);
        member.Remove(2, _actorId, _now);
        member.ClearDomainEvents();

        member.Restore(_actorId, _now);

        member.IsDeleted.Should().BeFalse();
        member.Status.Should().Be(AccountMemberStatus.Active);
        member.DomainEvents.Should().ContainSingle(e => e is AccountMemberRestoredDomainEvent);
    }

    [Fact]
    public void SoftDelete_ShouldMarkAsRemoved()
    {
        var member = AccountMember.Create(_accountId, _userId, AccountRole.Member, _actorId, _now);
        member.ClearDomainEvents();

        member.SoftDelete(_actorId, _now);

        member.IsDeleted.Should().BeTrue();
        member.Status.Should().Be(AccountMemberStatus.Removed);
    }
}
