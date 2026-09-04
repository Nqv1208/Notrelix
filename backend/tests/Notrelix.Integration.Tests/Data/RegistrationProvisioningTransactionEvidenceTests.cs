using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Features.Accounts.Provisioning;
using Notrelix.Application.Features.Accounts.Public.Commands;
using Notrelix.Application.Features.Identity.Registration.Commands.Register;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Data;

/// <summary>
/// TAC-IA-FLOW-02 / BOUND-TX-004 runtime proof (real PostgreSQL). Registration
/// provisions Identity User + personal Account + owner AccountMember inside the
/// caller's request transaction. This is the reviewed shared-transaction
/// exception (Decision B), not an endorsement of cross-context atomicity as a
/// general pattern.
///
/// Proof cases (status doc §I):
///   1. success  -> User + personal Account + owner member all commit
///   2. Accounts failure -> Identity User rolls back
///   3. request failure after Accounts mutation -> no orphan Account
///   4. duplicate registration -> no duplicate personal Account
///   5. registration event/outbox -> emitted only after committed registration
///
/// Cases 1-3 are driven through the real EfRequestDataSession so transaction
/// rollback/commit semantics are exercised against PostgreSQL, not a mock.
/// </summary>
[Collection("Database")]
public class RegistrationProvisioningTransactionEvidenceTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public RegistrationProvisioningTransactionEvidenceTests(PostgresTestContainer db)
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

    private (EfRequestDataSession Session, ApplicationDbContext Context) Create()
    {
        var tenant = SystemTenant();
        var context = _db.CreateContext(tenant);
        var session = new EfRequestDataSession(
            context,
            new RlsSessionContext(context, Options.Create(new RlsOptions()), tenant),
            NullLogger<EfRequestDataSession>.Instance);
        return (session, context);
    }

    private static IPasswordHasher StubHasher()
    {
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed-password");
        return hasher.Object;
    }

    private static IAuthSessionIssuer StubSessionIssuer()
    {
        var issuer = new Mock<IAuthSessionIssuer>();
        issuer
            .Setup(x => x.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, DateTimeOffset at, CancellationToken _) => new AuthResult
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAt = at.UtcDateTime,
                WorkspaceProvisioning = "pending"
            });
        return issuer.Object;
    }

    private static IDateTimeProvider FixedClock(DateTimeOffset time)
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(x => x.UtcNow).Returns(time);
        return clock.Object;
    }

    private static RegisterCommand NewRegister(string email = "boundtx004@example.com") => new()
    {
        Email = email,
        Password = "Password1!",
        Name = "Bound Tx"
    };

    [Fact]
    public async Task Registration_Success_CommitsUserAccountAndOwnerMemberAtomically()
    {
        var (session, context) = Create();
        var time = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

        await session.ExecuteAsync<object?>(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            async ct =>
            {
                var handler = new RegisterCommandHandler(
                    context,
                    new AccountProvisioningService(
                        context,
                        new AccountGrantProjectionServiceAdapter(new AccessGrantProjectionService(context))),
                    StubHasher(),
                    StubSessionIssuer(),
                    FixedClock(time),
                    new Notrelix.Application.Common.Messaging.IntegrationEventCollector());
                var result = await handler.Handle(NewRegister(), ct);
                result.Succeeded.Should().BeTrue();
                return null;
            },
            CancellationToken.None);

        context.ChangeTracker.Clear();
        (await context.Users.CountAsync()).Should().Be(1, "the Identity User must commit");
        var account = await context.Accounts.SingleAsync(a => a.Type == Notrelix.Domain.Accounts.Accounts.AccountType.Personal);
        account.Name.Should().Be("Bound Tx's Account");
        var member = await context.AccountMembers.SingleAsync(m => m.AccountId == account.Id);
        member.Role.Should().Be(Notrelix.Domain.Accounts.Members.AccountRole.Owner);
    }

    [Fact]
    public async Task Registration_WhenAccountsProvisioningThrows_RollsBackUser()
    {
        var (session, context) = Create();
        var time = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

        var failing = new Mock<IAccountProvisioningActions>();
        failing
            .Setup(x => x.ProvisionPersonalAccountAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("accounts-side failure"));

        var act = async () => await session.ExecuteAsync<object?>(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            async ct =>
            {
                var handler = new RegisterCommandHandler(
                    context,
                    failing.Object,
                    StubHasher(),
                    StubSessionIssuer(),
                    FixedClock(time),
                    new Notrelix.Application.Common.Messaging.IntegrationEventCollector());
                await handler.Handle(NewRegister(), ct);
                return null;
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        context.ChangeTracker.Clear();
        (await context.Users.Where(u => u.NormalizedEmail == "BOUNDTX004@EXAMPLE.COM").CountAsync())
            .Should().Be(0, "the Identity User must roll back when Accounts provisioning fails");
        (await context.Accounts.CountAsync()).Should().Be(0, "a failed provisioning must leave no Account behind");
    }

    [Fact]
    public async Task Registration_WhenRequestFailsAfterAccountsMutation_LeavesNoOrphanAccount()
    {
        var (session, context) = Create();
        var time = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

        var act = async () => await session.ExecuteAsync<object?>(
            new RequestDataSessionOptions(
                RequestDataAccess.Transactional,
                ApplyTenantScope: false,
                ApplyResourceScope: false,
                ExpectedVersion: null),
            async ct =>
            {
                var handler = new RegisterCommandHandler(
                    context,
                    new AccountProvisioningService(
                        context,
                        new AccountGrantProjectionServiceAdapter(new AccessGrantProjectionService(context))),
                    StubHasher(),
                    StubSessionIssuer(),
                    FixedClock(time),
                    new Notrelix.Application.Common.Messaging.IntegrationEventCollector());
                await handler.Handle(NewRegister(), ct);
                throw new InvalidOperationException("later request failure after Accounts mutation");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        context.ChangeTracker.Clear();
        (await context.Users.CountAsync()).Should().Be(0, "the User added before Accounts mutation must roll back");
        (await context.Accounts.CountAsync()).Should().Be(0, "no orphan Account may survive a rolled-back request");
        (await context.AccountMembers.CountAsync()).Should().Be(0);
    }
}
