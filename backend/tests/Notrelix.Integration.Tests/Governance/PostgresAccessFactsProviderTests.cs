using MediatR;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Common.Requests.Gates;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Common.Requests.Security;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Governance;

[Collection("Database")]
public sealed class PostgresAccessFactsProviderTests : IAsyncLifetime
{
    private sealed record VerifiedRequest
        : IRequest<string>, IAuthenticatedRequest, IGlobalRequest, INoDataRequest, IRequireVerifiedEmail;

    private sealed record SubscriptionRequest(string? MinimumTier)
        : IRequest<string>, IAuthenticatedRequest, IAccountRequest, IWriteRequest, IRequireSubscription;

    private readonly PostgresTestContainer _database;
    private DatabaseReset _reset = null!;

    public PostgresAccessFactsProviderTests(PostgresTestContainer database)
    {
        _database = database;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_database.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ResolveAsync_executes_on_the_active_transaction_and_returns_one_snapshot()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _database.CreateContext(tenant);
        await context.Database.OpenConnectionAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var provider = new PostgresAccessFactsProvider(
            context, TimeProvider.System, new PostgresPageAuthorizationFacts(context),
            new FakeBillingSubscriptionFacts());
        var descriptor = RequestDescriptorValidator.Create(typeof(VerifiedRequest));
        var snapshot = new ExecutionContextSnapshot(
            Guid.NewGuid(), null, null, null,
            ApplicationPrincipalKind.Authenticated,
            ApplicationScopeKind.Global,
            Guid.NewGuid().ToString("D"));

        var facts = await provider.ResolveAsync(
            descriptor, snapshot, new VerifiedRequest(), CancellationToken.None);

        facts.UserExists.Should().BeFalse();
        facts.EmailVerified.Should().BeFalse();
        facts.PermissionRules.Should().BeEmpty();
        context.Database.CurrentTransaction.Should().BeSameAs(transaction);
    }

    [Fact]
    public async Task ResolveAsync_without_subscription_requirement_never_consults_billing()
    {
        var billing = new FakeBillingSubscriptionFacts(satisfied: false);
        await using var scope = OpenScope(billing);
        var descriptor = RequestDescriptorValidator.Create(typeof(VerifiedRequest));
        var snapshot = AccountSnapshot();

        var facts = await scope.Provider.ResolveAsync(descriptor, snapshot, new VerifiedRequest(), CancellationToken.None);

        // No subscription gate on this request → the Billing decision seam is
        // not invoked and the neutral fact stays trivially satisfied.
        billing.CallCount.Should().Be(0);
        facts.SubscriptionRequirementSatisfied.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_subscription_requirement_forwards_scope_and_adopts_billing_decision()
    {
        var billing = new FakeBillingSubscriptionFacts(satisfied: false);
        await using var scope = OpenScope(billing);
        var descriptor = RequestDescriptorValidator.Create(typeof(SubscriptionRequest));
        var snapshot = AccountSnapshot();

        var facts = await scope.Provider.ResolveAsync(
            descriptor, snapshot, new SubscriptionRequest("Pro"), CancellationToken.None);

        // Billing answers the whole question; the provider adopts its boolean
        // verbatim and forwards the account scope + requested minimum tier.
        facts.SubscriptionRequirementSatisfied.Should().BeFalse();
        billing.CallCount.Should().Be(1);
        billing.LastAccountId.Should().Be(snapshot.AccountId);
        billing.LastMinimumTier.Should().Be("Pro");
    }

    [Fact]
    public async Task ResolveAsync_subscription_requirement_adopts_satisfied_decision()
    {
        var billing = new FakeBillingSubscriptionFacts(satisfied: true);
        await using var scope = OpenScope(billing);
        var descriptor = RequestDescriptorValidator.Create(typeof(SubscriptionRequest));
        var snapshot = AccountSnapshot();

        var facts = await scope.Provider.ResolveAsync(
            descriptor, snapshot, new SubscriptionRequest(MinimumTier: null), CancellationToken.None);

        facts.SubscriptionRequirementSatisfied.Should().BeTrue();
        billing.CallCount.Should().Be(1);
        billing.LastMinimumTier.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_declared_subscription_without_marker_fails_closed()
    {
        var billing = new FakeBillingSubscriptionFacts();
        await using var scope = OpenScope(billing);
        var descriptor = RequestDescriptorValidator.Create(typeof(SubscriptionRequest));
        var snapshot = AccountSnapshot();

        // The descriptor declares the gate, but a request that omits the marker
        // contract cannot be evaluated → fail closed rather than silently pass.
        var act = () => scope.Provider.ResolveAsync(descriptor, snapshot, new object(), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.SecurityMisconfigurationException>();
        billing.CallCount.Should().Be(0);
    }

    private AuthzScope OpenScope(FakeBillingSubscriptionFacts billing)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        var context = _database.CreateContext(tenant);
        context.Database.OpenConnection();
        var transaction = context.Database.BeginTransaction();
        var provider = new PostgresAccessFactsProvider(
            context, TimeProvider.System, new PostgresPageAuthorizationFacts(context), billing);
        return new AuthzScope(context, transaction, provider);
    }

    private static ExecutionContextSnapshot AccountSnapshot() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        null,
        null,
        ApplicationPrincipalKind.Authenticated,
        ApplicationScopeKind.Account,
        Guid.NewGuid().ToString("D"));

    private sealed class AuthzScope(
        Notrelix.Infrastructure.Data.ApplicationDbContext context,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        PostgresAccessFactsProvider provider) : IAsyncDisposable
    {
        public PostgresAccessFactsProvider Provider { get; } = provider;

        public async ValueTask DisposeAsync()
        {
            await transaction.DisposeAsync();
            await context.DisposeAsync();
        }
    }
}
