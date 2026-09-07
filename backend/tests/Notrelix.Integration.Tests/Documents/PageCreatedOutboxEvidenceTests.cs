using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.EventMappers.Documents;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Documents;

/// <summary>
/// TAC-DC-FLOW-01 producer side — the Documents page-created event chain
/// commits atomically: a valid Page.Create mutation raises the Documents-owned
/// Domain fact, the producer-owned mapper turns it into the registry identity
/// "page.created" V1 with the authoritative AccountId + WorkspaceId envelope
/// (TAC-FRZ-018), and the outbox record is written inside the same
/// SaveChanges. A rolled-back mutation leaves no committed outward delivery.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class PageCreatedOutboxEvidenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public PageCreatedOutboxEvidenceTests(PostgresTestContainer db)
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

    private sealed record PageGraph(Guid AccountId, Guid WorkspaceId, Guid AuthorId);

    private async Task<PageGraph> SeedWorkspaceAsync()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ownerId, "DC Page WS", $"dc-page-{Guid.NewGuid():N}", Now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        await seed.SaveChangesAsync();

        return new PageGraph(accountId, workspace.Id, ownerId);
    }

    private static DomainEventInterceptor CreateOutboxInterceptor()
    {
        return new DomainEventInterceptor(
            new FixedClock(Now),
            new EventTypeRegistry(),
            ClassificationPolicy.CreateBuilder().Build(),
            DeliveryPolicy.CreateBuilder().Build(),
            new CompositeIntegrationEventMapper(
                new ServiceCollection()
                    .AddScoped<IIntegrationEventMapper, PageEventMapper>()
                    .BuildServiceProvider()),
            new IntegrationEventCollector());
    }

    private static Page NewPageFact(PageGraph graph) =>
        Page.Create(graph.AccountId, graph.WorkspaceId, "Outbox Evidence Page", graph.AuthorId, Now);

    private async Task<MessagingOutboxMessage?> SinglePageOutboxAsync(Guid workspaceId, string messageName)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.Set<MessagingOutboxMessage>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(message => message.WorkspaceId == workspaceId
                && message.MessageName == messageName);
    }

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    [Fact]
    public async Task PageCreatedFact_OutboxIntent_CommitsAtomically_WithAuthoritativeEnvelope()
    {
        var graph = await SeedWorkspaceAsync();

        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        context.Pages.Add(NewPageFact(graph));
        await context.SaveChangesAsync();

        var outbox = await SinglePageOutboxAsync(graph.WorkspaceId, "page.created");
        outbox.Should().NotBeNull("a valid page mutation must stage exactly one producer-owned outward event");
        outbox!.SchemaVersion.Should().Be(1);
        outbox.AccountId.Should().Be(graph.AccountId);
        outbox.WorkspaceId.Should().Be(graph.WorkspaceId);

        outbox.PayloadJson.RootElement.GetProperty("accountId").GetGuid().Should().Be(graph.AccountId);
        outbox.PayloadJson.RootElement.GetProperty("workspaceId").GetGuid().Should().Be(graph.WorkspaceId);
    }

    [Fact]
    public async Task RolledBackPageCreation_NoCommittedOutwardDelivery()
    {
        var graph = await SeedWorkspaceAsync();

        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Pages.Add(NewPageFact(graph));
        await context.SaveChangesAsync();
        await transaction.RollbackAsync();

        var outbox = await SinglePageOutboxAsync(graph.WorkspaceId, "page.created");
        outbox.Should().BeNull("a rolled-back page mutation must leave no committed outward delivery");
    }
}
