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
/// STN-TX-001: executable evidence for the current cross-context mutation
/// behavior of the accounts membership provisioning seam. The invitation
/// acceptance request runs through a transactional data session, so a
/// mutation performed by the accounts provisioner and a later failure inside
/// the same request roll back together. This is evidence of today's
/// behavior, not an endorsement of cross-context atomicity.
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

    private (EfRequestDataSession Session, ApplicationDbContext Context, AccountMembershipProvisioner Provisioner) Create()
    {
        var context = _db.CreateContext(SystemTenant());
        var session = new EfRequestDataSession(
            context,
            new RlsSessionContext(context, Options.Create(new RlsOptions()), SystemTenant()),
            NullLogger<EfRequestDataSession>.Instance);
        var provisioner = new AccountMembershipProvisioner(
            context,
            new AccessGrantProjectionService(context));
        return (session, context, provisioner);
    }

    [Fact]
    public async Task MutationInsideTransactionalSession_CommitsWithSession()
    {
        var (session, context, provisioner) = Create();
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
                await provisioner.EnsureWorkspaceInviteeAccountMembershipAsync(
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
    public async Task FailureAfterProvisionerMutation_RollsBackAccountMember()
    {
        var (session, context, provisioner) = Create();
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
                await provisioner.EnsureWorkspaceInviteeAccountMembershipAsync(
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
}
