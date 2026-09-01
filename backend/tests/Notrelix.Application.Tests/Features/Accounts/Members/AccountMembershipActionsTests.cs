using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Accounts.Members.Services;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Tests.Features.Accounts.Members;

/// <summary>
/// TAC-IA-006 — Accounts Public target action behavior: valid provisioning,
/// idempotent duplicate (including twice inside one unsaved request
/// transaction), unknown account, non-admissible account lifecycle, and
/// non-active existing membership rejection. Uses a real EF change tracker so
/// the pending-add idempotency semantics are exercised honestly.
/// </summary>
public class AccountMembershipActionsTests
{
    private static readonly DateTimeOffset TestNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private TestAccountDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestAccountDbContext>()
            .UseInMemoryDatabase($"tac-ia-{Guid.NewGuid():N}")
            .Options;
        return new TestAccountDbContext(options);
    }

    private AccountMembershipActions CreateSut(TestAccountDbContext context)
        => new(context, Mock.Of<IAccessGrantProjectionService>());

    private static Account CreateAccount(AccountStatus status, Guid id)
    {
        var account = Account.Create(
            "Test Account",
            "test-account",
            AccountType.Team,
            Guid.CreateVersion7(),
            TestNow);

        typeof(Account).GetProperty(nameof(Account.Id))!.SetValue(account, id);
        typeof(Account).GetProperty(nameof(Account.Status))!.SetValue(account, status);
        return account;
    }

    [Fact]
    public async Task EnsureMembership_WithAdmissibleAccount_CreatesActiveMember()
    {
        var context = CreateContext();
        var sut = CreateSut(context);
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        context.Accounts.Add(CreateAccount(AccountStatus.Active, accountId));
        await context.SaveChangesAsync();

        await sut.EnsureWorkspaceInviteeMembershipAsync(
            accountId, userId, Guid.CreateVersion7(), TestNow, CancellationToken.None);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var member = await context.AccountMembers.SingleAsync(m => m.AccountId == accountId && m.UserId == userId);
        member.Status.Should().Be(AccountMemberStatus.Active);
    }

    [Fact]
    public async Task EnsureMembership_CalledTwiceInsideOneTransaction_IsIdempotentNoOp()
    {
        var context = CreateContext();
        var sut = CreateSut(context);
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var invitedBy = Guid.CreateVersion7();
        context.Accounts.Add(CreateAccount(AccountStatus.Active, accountId));
        await context.SaveChangesAsync();

        await sut.EnsureWorkspaceInviteeMembershipAsync(accountId, userId, invitedBy, TestNow, CancellationToken.None);
        await sut.EnsureWorkspaceInviteeMembershipAsync(
            accountId, userId, invitedBy, TestNow.AddMinutes(1), CancellationToken.None);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        (await context.AccountMembers
            .Where(m => m.AccountId == accountId && m.UserId == userId)
            .ToListAsync()).Should().ContainSingle(
            "duplicate acceptance inside one request must not create a second membership");
    }

    [Fact]
    public async Task EnsureMembership_RetryAcrossTransactions_DoesNotDuplicate()
    {
        var context = CreateContext();
        var sut = CreateSut(context);
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var invitedBy = Guid.CreateVersion7();
        context.Accounts.Add(CreateAccount(AccountStatus.Active, accountId));
        await context.SaveChangesAsync();

        await sut.EnsureWorkspaceInviteeMembershipAsync(accountId, userId, invitedBy, TestNow, CancellationToken.None);
        await context.SaveChangesAsync();
        await sut.EnsureWorkspaceInviteeMembershipAsync(accountId, userId, invitedBy, TestNow, CancellationToken.None);

        context.ChangeTracker.Clear();
        (await context.AccountMembers
            .Where(m => m.AccountId == accountId && m.UserId == userId)
            .ToListAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task EnsureMembership_WhenAccountMissing_ThrowsBusinessRule_AndAddsNothing()
    {
        var context = CreateContext();
        var sut = CreateSut(context);

        var act = async () => await sut.EnsureWorkspaceInviteeMembershipAsync(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), TestNow, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.RuleCode == "Accounts_Membership_AccountNotFound");
        context.AccountMembers.Local.Should().BeEmpty("the rejected mutation must leave no pending state");
    }

    [Theory]
    [InlineData(AccountStatus.Suspended)]
    [InlineData(AccountStatus.Closed)]
    public async Task EnsureMembership_WhenAccountNotAdmissible_ThrowsBusinessRule(AccountStatus status)
    {
        var context = CreateContext();
        var sut = CreateSut(context);
        var accountId = Guid.CreateVersion7();
        context.Accounts.Add(CreateAccount(status, accountId));
        await context.SaveChangesAsync();

        var act = async () => await sut.EnsureWorkspaceInviteeMembershipAsync(
            accountId, Guid.CreateVersion7(), Guid.CreateVersion7(), TestNow, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.RuleCode == "Accounts_Membership_AccountNotAdmissible");
        context.AccountMembers.Local.Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureMembership_WhenExistingMembershipNotActive_ThrowsBusinessRule()
    {
        var context = CreateContext();
        var sut = CreateSut(context);
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        context.Accounts.Add(CreateAccount(AccountStatus.Active, accountId));
        var suspended = AccountMember.Create(accountId, userId, AccountRole.Member, Guid.CreateVersion7(), TestNow);
        typeof(AccountMember).GetProperty(nameof(AccountMember.Status))!
            .SetValue(suspended, AccountMemberStatus.Suspended);
        context.AccountMembers.Add(suspended);
        await context.SaveChangesAsync();

        var act = async () => await sut.EnsureWorkspaceInviteeMembershipAsync(
            accountId, userId, Guid.CreateVersion7(), TestNow, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.RuleCode == "Accounts_Membership_NotActive");
    }
}
