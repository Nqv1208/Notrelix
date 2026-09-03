using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Accounts.Commands.RenameAccount;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Data;

/// <summary>
/// TAC-IA-FLOW-06 evidence: RenameAccount is an Account-scoped mutation that
/// (1) operates only on the tenant-resolved Account (trusted Account scope),
/// (2) fails closed on a closed account, (3) is a semantic no-op for the same
/// name, (4) uses the global aggregate Version concurrency token so two
/// concurrent conflicting renames resolve deterministically (one wins, the
/// other conflicts — no lost update), and (5) rolls back atomically with the
/// caller's request transaction when a later step fails.
/// </summary>
[Collection("Database")]
public class RenameAccountEvidenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset FixedTime = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerUserId = Guid.CreateVersion7();

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public RenameAccountEvidenceTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private static Domain.Accounts.Accounts.Account CreateAccount(Guid accountId, string name = "Acme Inc")
    {
        var account = Domain.Accounts.Accounts.Account.Create(
            name,
            "acme-inc",
            AccountType.Team,
            OwnerUserId,
            FixedTime);
        typeof(Domain.Accounts.Accounts.Account)
            .GetProperty(nameof(Domain.Accounts.Accounts.Account.Id))!
            .SetValue(account, accountId);
        return account;
    }

    private static Domain.Accounts.Accounts.Account CreateClosedAccount(Guid accountId)
    {
        var account = CreateAccount(accountId, "Closed Co");
        typeof(Domain.Accounts.Accounts.Account)
            .GetMethod(nameof(Domain.Accounts.Accounts.Account.Archive))!
            .Invoke(account, new object?[] { OwnerUserId, FixedTime });
        account.Status.Should().Be(AccountStatus.Closed);
        return account;
    }

    private (EfRequestDataSession Session, ApplicationDbContext Context) Create(Guid accountId, Guid actingUserId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(accountId, actingUserId);
        var context = _db.CreateContext(tenant);
        var session = new EfRequestDataSession(
            context,
            new RlsSessionContext(context, Options.Create(new RlsOptions()), tenant),
            NullLogger<EfRequestDataSession>.Instance);
        return (session, context);
    }

    private static RenameAccountCommandHandler CreateHandler(ApplicationDbContext context, Guid accountId, Guid userId)
    {
        var request = new FakeCurrentRequestContext();
        request.AsUser(userId);
        request.Tenant.SetAccount(accountId, userId);
        return new RenameAccountCommandHandler(
            context,
            request,
            FakeDateTimeProvider.WithFixedTime(FixedTime));
    }

    [Fact]
    public async Task Rename_WhenAccountActiveAndSameScope_RenamesAndBumpsVersion()
    {
        var accountId = Guid.CreateVersion7();
        var (session, context) = Create(accountId, OwnerUserId);
        context.Accounts.Add(CreateAccount(accountId));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = CreateHandler(context, accountId, OwnerUserId);
        var result = await session.ExecuteAsync<Result>(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            async ct =>
            {
                var r = await handler.Handle(new RenameAccountCommand("Acme Renamed"), ct);
                return r;
            },
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();

        context.ChangeTracker.Clear();
        var account = await context.Accounts.SingleAsync(a => a.Id == accountId);
        account.Name.Should().Be("Acme Renamed");
        account.Version.Should().Be(2, "a real rename must increment the aggregate version");
    }

    [Fact]
    public async Task Rename_WhenSameName_IsNoOp()
    {
        var accountId = Guid.CreateVersion7();
        var (session, context) = Create(accountId, OwnerUserId);
        context.Accounts.Add(CreateAccount(accountId));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = CreateHandler(context, accountId, OwnerUserId);
        var result = await session.ExecuteAsync<Result>(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            async ct => await handler.Handle(new RenameAccountCommand("Acme Inc"), ct),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        context.ChangeTracker.Clear();
        var account = await context.Accounts.SingleAsync(a => a.Id == accountId);
        account.Name.Should().Be("Acme Inc");
        account.Version.Should().Be(1, "a same-name rename is a semantic no-op and must not bump version");
    }

    [Fact]
    public async Task Rename_WhenAnotherAccountsScope_ThrowsNotFound()
    {
        var accountId = Guid.CreateVersion7();
        var otherAccountId = Guid.CreateVersion7();
        var (session, context) = Create(otherAccountId, OwnerUserId);
        context.Accounts.Add(CreateAccount(accountId));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Acting under a different account scope than the persisted account.
        var handler = CreateHandler(context, otherAccountId, OwnerUserId);

        var act = async () => await session.ExecuteAsync<object?>(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            async ct =>
            {
                await handler.Handle(new RenameAccountCommand("Acme Renamed"), ct);
                return null;
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>(
            "the rename must only ever see the tenant-resolved account (trusted Account scope)");
    }

    [Fact]
    public async Task Rename_WhenAccountClosed_FailsClosedAndLeavesUnchanged()
    {
        var accountId = Guid.CreateVersion7();
        var (session, context) = Create(accountId, OwnerUserId);
        context.Accounts.Add(CreateClosedAccount(accountId));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = CreateHandler(context, accountId, OwnerUserId);
        var act = async () => await session.ExecuteAsync<object?>(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            async ct =>
            {
                await handler.Handle(new RenameAccountCommand("Acme Renamed"), ct);
                return null;
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task Rename_ThenFailureLater_InSameTransaction_RollsBack()
    {
        var accountId = Guid.CreateVersion7();
        var (session, context) = Create(accountId, OwnerUserId);
        context.Accounts.Add(CreateAccount(accountId));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = CreateHandler(context, accountId, OwnerUserId);
        var act = async () => await session.ExecuteAsync<object?>(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            async ct =>
            {
                await handler.Handle(new RenameAccountCommand("Acme Renamed"), ct);
                throw new InvalidOperationException("later step failure");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        context.ChangeTracker.Clear();
        var account = await context.Accounts.SingleAsync(a => a.Id == accountId);
        account.Name.Should().Be("Acme Inc", "the rename must roll back with the request transaction");
        account.Version.Should().Be(1);
    }

    [Fact]
    public async Task Rename_TwoConcurrentConflictingRenames_OneWinsOneConflicts()
    {
        var accountId = Guid.CreateVersion7();

        await using var firstContext = _db.CreateContext(SystemTenant());
        firstContext.Accounts.Add(CreateAccount(accountId));
        await firstContext.SaveChangesAsync();

        await using var ctxA = _db.CreateContext(SystemTenant());
        await using var ctxB = _db.CreateContext(SystemTenant());

        var handlerA = CreateHandler(ctxA, accountId, OwnerUserId);
        var handlerB = CreateHandler(ctxB, accountId, OwnerUserId);

        // Both load the account at version 1 before either saves.
        var accountA = await ctxA.Accounts.SingleAsync(a => a.Id == accountId);
        var accountB = await ctxB.Accounts.SingleAsync(a => a.Id == accountId);
        accountA.Version.Should().Be(1);
        accountB.Version.Should().Be(1);

        await handlerA.Handle(new RenameAccountCommand("Name A"), CancellationToken.None);
        await handlerB.Handle(new RenameAccountCommand("Name B"), CancellationToken.None);

        await ctxA.SaveChangesAsync();

        DbUpdateConcurrencyException? conflict = null;
        try
        {
            await ctxB.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            conflict = ex;
        }

        conflict.Should().NotBeNull(
            "the global aggregate Version concurrency token must reject the stale conflicting rename");
        accountB.Name.Should().Be("Name B");

        await using var verify = _db.CreateContext(SystemTenant());
        var persisted = await verify.Accounts.SingleAsync(a => a.Id == accountId);
        persisted.Name.Should().Be("Name A", "one rename must win; the stale one must not overwrite it");
        persisted.Version.Should().Be(2);
    }
}
