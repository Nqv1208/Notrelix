using FluentAssertions;
using Notrelix.Domain.Accounts.Accounts;

namespace Notrelix.Domain.Tests.Accounts;

public class AccountTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldSucceed()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Team, _userId, _now);

        account.Name.Should().Be("My Account");
        account.Slug.Should().Be("my-account");
        account.Type.Should().Be(AccountType.Team);
        account.Status.Should().Be(AccountStatus.Active);
        account.DomainEvents.Should().ContainSingle(e => e is AccountCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithLegalName_ShouldSet()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Enterprise, _userId, _now, "My Corp Inc.");

        account.LegalName.Should().Be("My Corp Inc.");
    }

    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        var act = () => Account.Create("", "my-account", AccountType.Team, _userId, _now);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_EmptySlug_ShouldThrow()
    {
        var act = () => Account.Create("My Account", "", AccountType.Team, _userId, _now);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Rename_ShouldSucceed_AndRaiseEvent()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Team, _userId, _now);
        account.ClearDomainEvents();

        account.Rename("New Name", _userId, _now);

        account.Name.Should().Be("New Name");
        account.DomainEvents.Should().ContainSingle(e => e is AccountRenamedDomainEvent);
    }

    [Fact]
    public void Rename_SameName_ShouldNotRaiseEvent()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Team, _userId, _now);
        account.ClearDomainEvents();

        account.Rename("My Account", _userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Rename_ClosedAccount_ShouldThrow()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Team, _userId, _now);
        account.Archive(_userId, _now);

        var act = () => account.Rename("New Name", _userId, _now);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot rename a closed account.");
    }

    [Fact]
    public void Archive_ShouldSucceed()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Team, _userId, _now);
        account.ClearDomainEvents();

        account.Archive(_userId, _now);

        account.Status.Should().Be(AccountStatus.Closed);
        account.DomainEvents.Should().ContainSingle(e => e is AccountArchivedDomainEvent);
    }

    [Fact]
    public void Suspend_ShouldSucceed()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Team, _userId, _now);
        account.ClearDomainEvents();

        account.Suspend(_userId, _now);

        account.Status.Should().Be(AccountStatus.Suspended);
        account.DomainEvents.Should().ContainSingle(e => e is AccountSuspendedDomainEvent);
    }

    [Fact]
    public void Suspend_AlreadySuspended_ShouldNotRaiseEvent()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Team, _userId, _now);
        account.Suspend(_userId, _now);
        account.ClearDomainEvents();

        account.Suspend(_userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Activate_ShouldRestoreToActive()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Team, _userId, _now);
        account.Suspend(_userId, _now);
        account.ClearDomainEvents();

        account.Activate(_userId, _now);

        account.Status.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public void SoftDelete_ShouldMarkAsSoftDeleted()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Team, _userId, _now);
        account.ClearDomainEvents();

        account.SoftDelete(_userId, _now);

        account.IsDeleted.Should().BeTrue();
        account.Status.Should().Be(AccountStatus.SoftDeleted);
        account.DomainEvents.Should().ContainSingle(e => e is AccountSoftDeletedDomainEvent);
    }

    [Fact]
    public void SoftDelete_DeletedAccount_ShouldNotRaiseEvent()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Team, _userId, _now);
        account.SoftDelete(_userId, _now);
        account.ClearDomainEvents();

        account.SoftDelete(_userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_ShouldRestoreToActive()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Team, _userId, _now);
        account.SoftDelete(_userId, _now);
        account.ClearDomainEvents();

        account.Restore(_userId, _now);

        account.IsDeleted.Should().BeFalse();
        account.Status.Should().Be(AccountStatus.Active);
        account.DomainEvents.Should().ContainSingle(e => e is AccountRestoredDomainEvent);
    }

    [Fact]
    public void Restore_ActiveAccount_ShouldNotRaiseEvent()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Team, _userId, _now);
        account.ClearDomainEvents();

        account.Restore(_userId, _now);

        account.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdatePlanCode_ShouldSucceed()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Team, _userId, _now);
        account.UpdatePlanCode("enterprise", _userId, _now);

        account.PlanCode.Should().Be("enterprise");
    }

    [Fact]
    public void UpdateDefaultRegion_ShouldSucceed()
    {
        var account = Account.Create("My Account", "my-account", AccountType.Team, _userId, _now);
        account.UpdateDefaultRegion("us-east-1", _userId, _now);

        account.DefaultRegionCode.Should().Be("us-east-1");
    }
}
