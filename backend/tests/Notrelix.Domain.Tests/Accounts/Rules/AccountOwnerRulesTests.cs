using FluentAssertions;
using Notrelix.Domain.Accounts.Rules;

namespace Notrelix.Domain.Tests.Accounts;

public class AccountOwnerRulesTests
{
    [Fact]
    public void EnsureCanDowngradeOwner_OwnerToNonOwner_SingleOwner_ShouldThrow()
    {
        var act = () => AccountOwnerRules.EnsureCanDowngradeOwner(AccountRole.Owner, AccountRole.Member, 1);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot downgrade the last owner of the account.");
    }

    [Fact]
    public void EnsureCanDowngradeOwner_OwnerToNonOwner_MultipleOwners_ShouldNotThrow()
    {
        var act = () => AccountOwnerRules.EnsureCanDowngradeOwner(AccountRole.Owner, AccountRole.Member, 2);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanDowngradeOwner_NonOwnerToOwner_ShouldNotThrow()
    {
        var act = () => AccountOwnerRules.EnsureCanDowngradeOwner(AccountRole.Member, AccountRole.Owner, 1);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanSuspendOwner_OwnerRole_SingleOwner_ShouldThrow()
    {
        var act = () => AccountOwnerRules.EnsureCanSuspendOwner(AccountRole.Owner, 1);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot suspend the last owner of the account.");
    }

    [Fact]
    public void EnsureCanSuspendOwner_OwnerRole_MultipleOwners_ShouldNotThrow()
    {
        var act = () => AccountOwnerRules.EnsureCanSuspendOwner(AccountRole.Owner, 2);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanSuspendOwner_NonOwner_ShouldNotThrow()
    {
        var act = () => AccountOwnerRules.EnsureCanSuspendOwner(AccountRole.Member, 0);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanRemoveOwner_OwnerRole_SingleOwner_ShouldThrow()
    {
        var act = () => AccountOwnerRules.EnsureCanRemoveOwner(AccountRole.Owner, 1);
        act.Should().Throw<BusinessRuleException>().WithMessage("Cannot remove the last owner of the account.");
    }

    [Fact]
    public void EnsureCanRemoveOwner_OwnerRole_MultipleOwners_ShouldNotThrow()
    {
        var act = () => AccountOwnerRules.EnsureCanRemoveOwner(AccountRole.Owner, 2);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanRemoveOwner_NonOwner_ShouldNotThrow()
    {
        var act = () => AccountOwnerRules.EnsureCanRemoveOwner(AccountRole.Member, 0);
        act.Should().NotThrow();
    }
}
