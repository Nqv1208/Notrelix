using Notrelix.Domain.Common;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Operator;
using Notrelix.Application.Tests.Features.Billing;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Accounts.Events;

namespace Notrelix.Application.Tests.Features.Operators;

public class OperatorAccountServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly List<Account> _accounts = [];
    private readonly Mock<IAccountDbContext> _context = new();
    private readonly Mock<IOperatorAccountSummaryReader> _summary = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly RecordingAudit _audit = new();
    private readonly CountingCommit _commit = new();

    public OperatorAccountServiceTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _context.Setup(c => c.Accounts).Returns(TestBillingDbSet.Create(_accounts).Object);
        _summary.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new OperatorAccountSummary(7, 3));
    }

    private OperatorAccountService Service() =>
        new(_context.Object, _summary.Object, _audit, _commit, new FakeOperator(), _clock.Object);

    private Account NewAccount(string name = "Acme")
    {
        var account = Account.Create(name, name.ToLowerInvariant(), AccountType.Team, Guid.CreateVersion7(), Now.AddDays(-5));
        ((IHasDomainEvents)account).ClearDomainEvents();
        _accounts.Add(account);
        return account;
    }

    [Fact]
    public async Task Suspending_an_active_account_sets_Suspended_raises_one_event_and_audits_before_and_after()
    {
        var account = NewAccount();

        var result = await Service().SuspendAsync(account.Id, "fraud review", account.Version, CancellationToken.None);

        Assert.Equal("Suspended", result.Status);
        Assert.Equal(2, result.Version);
        Assert.Single(account.DomainEvents.OfType<AccountSuspendedDomainEvent>());
        var record = Assert.Single(_audit.Records);
        Assert.Equal("account.suspend", record.Action);
        Assert.Equal("fraud review", record.Reason);
        Assert.Equal(account.Id, record.AccountId);
        Assert.Contains("Active", System.Text.Json.JsonSerializer.Serialize(record.Before));
        Assert.Contains("Suspended", System.Text.Json.JsonSerializer.Serialize(record.After));
        Assert.Equal(1, _commit.Commits);
        Assert.Equal(FakeOperator.Id, account.UpdatedBy);
    }

    [Fact]
    public async Task Suspending_an_already_suspended_account_is_a_no_op_with_no_audit_and_no_commit()
    {
        var account = NewAccount();
        await Service().SuspendAsync(account.Id, "first", account.Version, CancellationToken.None);
        _audit.Records.Clear();
        var commits = _commit.Commits;

        var result = await Service().SuspendAsync(account.Id, "again", account.Version, CancellationToken.None);

        Assert.Equal("Suspended", result.Status);
        Assert.Empty(_audit.Records);
        Assert.Equal(commits, _commit.Commits);
    }

    [Fact]
    public async Task A_stale_version_is_a_conflict_and_changes_nothing()
    {
        var account = NewAccount();

        await Assert.ThrowsAsync<ConflictException>(() =>
            Service().SuspendAsync(account.Id, "why", account.Version + 5, CancellationToken.None));

        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Empty(_audit.Records);
        Assert.Equal(0, _commit.Commits);
    }

    [Fact]
    public async Task A_missing_reason_is_refused_before_anything_is_saved()
    {
        var account = NewAccount();

        await Assert.ThrowsAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>(() =>
            Service().SuspendAsync(account.Id, "  ", account.Version, CancellationToken.None));

        Assert.Equal(0, _commit.Commits);
    }

    [Fact]
    public async Task A_closed_account_cannot_be_suspended_and_a_suspension_cannot_reopen_it()
    {
        var account = NewAccount();
        account.Archive(Guid.CreateVersion7(), Now);

        await Assert.ThrowsAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>(() =>
            Service().SuspendAsync(account.Id, "x", account.Version, CancellationToken.None));
        await Assert.ThrowsAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>(() =>
            Service().LiftSuspensionAsync(account.Id, "x", account.Version, CancellationToken.None));
    }

    [Fact]
    public async Task Lifting_a_suspension_makes_the_account_Active_again_and_audits_it()
    {
        var account = NewAccount();
        account.Suspend(Guid.CreateVersion7(), Now.AddHours(-1));

        var result = await Service().LiftSuspensionAsync(account.Id, "cleared", account.Version, CancellationToken.None);

        Assert.Equal("Active", result.Status);
        Assert.Equal("account.lift-suspension", Assert.Single(_audit.Records).Action);
    }

    [Fact]
    public async Task Lifting_on_an_active_account_is_a_no_op()
    {
        var account = NewAccount();

        var result = await Service().LiftSuspensionAsync(account.Id, "nothing", account.Version, CancellationToken.None);

        Assert.Equal("Active", result.Status);
        Assert.Empty(_audit.Records);
        Assert.Equal(0, _commit.Commits);
    }

    [Fact]
    public async Task An_unknown_or_deleted_account_is_not_found_and_a_non_positive_version_is_invalid()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Service().SuspendAsync(Guid.CreateVersion7(), "x", 1, CancellationToken.None));
        var account = NewAccount();
        await Assert.ThrowsAsync<ValidationException>(() => Service().SuspendAsync(account.Id, "x", 0, CancellationToken.None));
    }

    [Fact]
    public async Task The_detail_shows_identity_status_and_two_counts_only()
    {
        var account = NewAccount();

        var detail = await Service().GetAsync(account.Id, CancellationToken.None);

        Assert.Equal(7, detail.ActiveMembers);
        Assert.Equal(3, detail.Workspaces);
        Assert.Equal(["Account", "ActiveMembers", "Workspaces"], typeof(OperatorAccountDetailDto).GetProperties().Select(p => p.Name));
        Assert.Equal(["Id", "Name", "Slug", "Status", "Version", "CreatedAt"], typeof(OperatorAccountDto).GetProperties().Select(p => p.Name));
    }

    [Fact]
    public async Task The_list_searches_by_name_or_slug_and_skips_nothing_it_may_not_show()
    {
        NewAccount("Acme");
        NewAccount("Globex");

        var page = await Service().ListAsync("glo", 0, 25, CancellationToken.None);

        Assert.Equal("Globex", Assert.Single(page.Items).Name);
        Assert.Equal(2, (await Service().ListAsync(null, 0, 25, CancellationToken.None)).Total);
    }
}
