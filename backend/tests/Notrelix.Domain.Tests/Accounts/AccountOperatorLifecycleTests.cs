using FluentAssertions;
using Notrelix.Domain.Accounts;

namespace Notrelix.Domain.Tests.Accounts;

public class AccountOperatorLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Actor = Guid.CreateVersion7();

    private static Account New()
    {
        var account = Account.Create("Acme", "acme", AccountType.Team, Actor, Now.AddDays(-3));
        ((IHasDomainEvents)account).ClearDomainEvents();
        return account;
    }

    [Fact]
    public void Suspend_sets_status_bumps_the_version_and_raises_one_event_with_the_previous_status_and_reason()
    {
        var account = New();
        var version = account.Version;

        account.Suspend(Actor, Now, "fraud review");

        account.Status.Should().Be(AccountStatus.Suspended);
        account.Version.Should().Be(version + 1);
        var raised = account.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<AccountSuspendedDomainEvent>().Subject;
        raised.PreviousStatus.Should().Be(AccountStatus.Active);
        raised.Reason.Should().Be("fraud review");
    }

    [Fact]
    public void Suspending_a_suspended_account_is_a_no_op()
    {
        var account = New();
        account.Suspend(Actor, Now);
        ((IHasDomainEvents)account).ClearDomainEvents();
        var version = account.Version;

        account.Suspend(Actor, Now.AddMinutes(1), "again");

        account.Version.Should().Be(version);
        account.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void A_closed_account_cannot_be_suspended_and_state_stays_unchanged()
    {
        var account = New();
        account.Archive(Actor, Now);
        var version = account.Version;

        var act = () => account.Suspend(Actor, Now.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(AccountRuleCodes.Accounts_Account_ClosedCannotBeSuspended);
        account.Status.Should().Be(AccountStatus.Closed);
        account.Version.Should().Be(version);
    }

    [Fact]
    public void Lifting_a_suspension_activates_and_raises_one_event_and_a_closed_account_is_never_reopened_this_way()
    {
        var account = New();
        account.Suspend(Actor, Now);
        ((IHasDomainEvents)account).ClearDomainEvents();

        account.Activate(Actor, Now.AddHours(1));

        account.Status.Should().Be(AccountStatus.Active);
        account.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<AccountActivatedDomainEvent>();

        var closed = New();
        closed.Archive(Actor, Now);
        var act = () => closed.Activate(Actor, Now.AddHours(1));
        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(AccountRuleCodes.Accounts_Account_ClosedCannotBeActivated);
    }

    [Fact]
    public void Activating_an_active_account_is_a_no_op()
    {
        var account = New();
        var version = account.Version;

        account.Activate(Actor, Now);

        account.Version.Should().Be(version);
        account.DomainEvents.Should().BeEmpty();
    }
}
