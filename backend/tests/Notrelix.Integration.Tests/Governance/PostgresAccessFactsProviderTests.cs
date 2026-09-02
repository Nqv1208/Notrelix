using MediatR;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Requests.Execution;
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
            context, TimeProvider.System, new FakeResourceAuthorizationFactsProvider());
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
}
