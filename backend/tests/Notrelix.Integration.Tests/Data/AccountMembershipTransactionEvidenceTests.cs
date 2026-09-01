using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Features.Accounts.Members.Services;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Data;

/// <summary>
/// TAC-TX-001 / BOUND-TX-002 evidence: the Accounts Public target action
/// (IAccountMembershipActions) executes inside the caller's request
/// transaction, so an Account-side mutation and a later Workspace-side
/// failure roll back together. Duplicate acceptance under the same identity
/// is a semantic no-op and must not create a second membership. This is the
/// reviewed shared-transaction exception (Decision B), not an endorsement of
/// cross-context atomicity as a general pattern.
/// </summary>
[Collection("Database")]
public class AccountMembershipTransactionEvidenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset FixedTime = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public AccountMembershipTransactionEvidenceTests(PostgresTestContainer db)
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

    private (EfRequestDataSession Session, ApplicationDbContext Context, AccountMembershipActions Actions) Create()
    {
        var context = _db.CreateContext(SystemTenant());
        var session = new EfRequestDataSession(
            context,
            new RlsSessionContext(context, Options.Create(new RlsOptions()), SystemTenant()),
            NullLogger<EfRequestDataSession>.Instance);
        var actions = new AccountMembershipActions(
            context,
            new AccessGrantProjectionService(context));
        return (session, context, actions);
    }

    [Fact]
    public async Task MutationInsideTransactionalSession_CommitsWithSession()
    {
        var (session, context, actions) = Create();
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        await session.ExecuteAsync<object?>(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            async ct =>
            {
                await actions.EnsureWorkspaceInviteeMembershipAsync(
                    accountId, userId, Guid.CreateVersion7(), FixedTime, ct);
                return null;
            },
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var member = await context.AccountMembers
            .SingleAsync(m => m.AccountId == accountId && m.UserId == userId);
        member.Status.Should().Be(AccountMemberStatus.Active);
    }

    [Fact]
    public async Task FailureAfterAccountMutation_RollsBackAccountMember()
    {
        var (session, context, actions) = Create();
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        var act = async () => await session.ExecuteAsync<object?>(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            async ct =>
            {
                await actions.EnsureWorkspaceInviteeMembershipAsync(
                    accountId, userId, Guid.CreateVersion7(), FixedTime, ct);

                // Simulate a later workspace-side failure inside the same request.
                throw new InvalidOperationException("workspace-side failure");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        context.ChangeTracker.Clear();
        var member = await context.AccountMembers
            .FirstOrDefaultAsync(m => m.AccountId == accountId && m.UserId == userId);
        member.Should().BeNull("the accounts mutation must roll back with the request transaction");
    }

    [Fact]
    public async Task DuplicateAcceptanceAttempt_IsIdempotentNoOp()
    {
        var (session, context, actions) = Create();
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var invitedBy = Guid.CreateVersion7();

        await session.ExecuteAsync<object?>(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            async ct =>
            {
                await actions.EnsureWorkspaceInviteeMembershipAsync(
                    accountId, userId, invitedBy, FixedTime, ct);
                await actions.EnsureWorkspaceInviteeMembershipAsync(
                    accountId, userId, invitedBy, FixedTime.AddMinutes(1), ct);
                return null;
            },
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var members = await context.AccountMembers
            .Where(m => m.AccountId == accountId && m.UserId == userId)
            .ToListAsync();
        members.Should().ContainSingle(
            "duplicate invitation acceptance must not create a second account membership");
        members.Single().Status.Should().Be(AccountMemberStatus.Active);
    }
}
