using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.EventMappers.Documents;
using Notrelix.Domain.Common;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.Pages.Events;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Documents;

/// <summary>
/// TAC-DC-FLOW-02 producer side — the page.archived event chain commits
/// atomically with the Page.Archive lifecycle mutation, and the
/// already-archived no-op emits no second outward delivery.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class PageArchivedOutboxEvidenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public PageArchivedOutboxEvidenceTests(PostgresTestContainer db)
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
        var workspace = Workspace.Create(accountId, ownerId, "DC Page Archive WS", $"dc-arch-{Guid.NewGuid():N}", Now);

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

    private static Page NewActivePage(PageGraph graph) =>
        Page.Create(graph.AccountId, graph.WorkspaceId, "Archive Outbox Page", graph.AuthorId, Now);

    private async Task<int> CountArchivedOutboxAsync(Guid workspaceId)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.Set<MessagingOutboxMessage>()
            .IgnoreQueryFilters()
            .CountAsync(message => message.WorkspaceId == workspaceId
                && message.MessageName == "page.archived");
    }

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    [Fact]
    public async Task PageArchivedFact_OutboxIntent_CommitsAtomically_WithAuthoritativeEnvelope()
    {
        var graph = await SeedWorkspaceAsync();

        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        var page = NewActivePage(graph);
        context.Pages.Add(page);
        await context.SaveChangesAsync();

        page.Archive(graph.AuthorId, Now);
        await context.SaveChangesAsync();

        (await CountArchivedOutboxAsync(graph.WorkspaceId)).Should().Be(1,
            "the archive mutation must stage exactly one producer-owned outward event");

        await using var probe = _db.CreateContext(SystemTenant());
        var outbox = await probe.Set<MessagingOutboxMessage>()
            .IgnoreQueryFilters()
            .SingleAsync(message => message.WorkspaceId == graph.WorkspaceId
                && message.MessageName == "page.archived");
        outbox.AccountId.Should().Be(graph.AccountId);
        outbox.WorkspaceId.Should().Be(graph.WorkspaceId);
    }

    /// <summary>
    /// Companion to the constraint-failure proof — an explicit transaction
    /// rollback after a successful SaveChanges. This is NOT a database
    /// failure proof (the database never failed); it documents that an
    /// aborted transaction discards both the page mutation and its staged
    /// outward delivery. State is asserted from a fresh committed context.
    /// </summary>
    [Fact]
    public async Task ManualTransactionRollback_DiscardsPageMutationAndStagedOutbox()
    {
        var graph = await SeedWorkspaceAsync();

        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        await using var transaction = await context.Database.BeginTransactionAsync();
        var page = NewActivePage(graph);
        context.Pages.Add(page);
        await context.SaveChangesAsync();
        page.Archive(graph.AuthorId, Now);
        await context.SaveChangesAsync();
        await transaction.RollbackAsync();

        (await CountArchivedOutboxAsync(graph.WorkspaceId)).Should().Be(0,
            "a rolled-back archive mutation must leave no committed outward delivery");
    }

    /// <summary>
    /// TAC-DC-FLOW-02 failure matrix — a REAL deterministic database failure
    /// inside the same SaveChanges batch as the archive mutation and its
    /// outward enrollment: a duplicate workspace-membership insert violates
    /// the unique index (same failure class as the slug-race proof), the
    /// PostgresException aborts the actual SaveChanges transaction, and a
    /// fresh committed context proves the page stays Active with zero outward
    /// facts. No manual rollback, no synthetic failure.
    /// </summary>
    [Fact]
    public async Task RealDbConstraintFailure_AbortsArchiveMutationAndOutboxEnrollment()
    {
        var graph = await SeedWorkspaceAsync();
        Guid pageId;

        await using (var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor()))
        {
            var page = NewActivePage(graph);
            context.Pages.Add(page);
            context.WorkspaceMembers.Add(Domain.Workspaces.Members.WorkspaceMember.Create(
                graph.AccountId, graph.WorkspaceId, graph.AuthorId,
                Domain.Workspaces.Members.WorkspaceRole.Owner, graph.AuthorId, Now));
            await context.SaveChangesAsync();
            pageId = page.Id;
        }

        await using (var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor()))
        {
            var page = await context.Pages.IgnoreQueryFilters().SingleAsync(p => p.Id == pageId);
            page.Archive(graph.AuthorId, Now);

            // Deterministic PostgreSQL failure inside the SAME SaveChanges:
            // the unique index idx_workspace_members_workspace_user rejects
            // the duplicate (workspace_id, user_id) row, aborting the whole
            // batch — the archive update, the DomainEventLog row, and the
            // staged page.archived outbox entry share one fate.
            context.WorkspaceMembers.Add(Domain.Workspaces.Members.WorkspaceMember.Create(
                graph.AccountId, graph.WorkspaceId, graph.AuthorId,
                Domain.Workspaces.Members.WorkspaceRole.Owner, graph.AuthorId, Now));

            var act = () => context.SaveChangesAsync();
            var thrown = (await act.Should().ThrowAsync<DbUpdateException>()).Which;
            var pg = thrown.InnerException.Should().BeAssignableTo<Npgsql.PostgresException>().Which;
            pg.ConstraintName.Should().Be(
                "idx_workspace_members_workspace_user",
                "the failure must originate from PostgreSQL, not an EF-side save error");
        }

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.Pages.IgnoreQueryFilters().SingleAsync(p => p.Id == pageId)).Status
            .Should().Be(PageStatus.Active,
            "the failed save must leave the committed lifecycle state Active");
        (await CountArchivedOutboxAsync(graph.WorkspaceId)).Should().Be(0,
            "no outward fact may survive a failed enrollment inside the commit");
    }

    [Fact]
    public async Task AlreadyArchivedPage_NoSecondOutwardDelivery()
    {
        var graph = await SeedWorkspaceAsync();

        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        var page = NewActivePage(graph);
        context.Pages.Add(page);
        await context.SaveChangesAsync();
        page.Archive(graph.AuthorId, Now);
        await context.SaveChangesAsync();

        page.Archive(graph.AuthorId, Now);
        await context.SaveChangesAsync();

        (await CountArchivedOutboxAsync(graph.WorkspaceId)).Should().Be(1,
            "the documented archive no-op must not emit a second fact");
    }

    /// <summary>
    /// TAC-DC-FLOW-02 failure matrix — an outward-enrollment failure inside
    /// the interceptor chain fails the actual SaveChanges: the mutation never
    /// commits and no partial outward delivery exists. This is the real
    /// interceptor path, not a synthetic failure after commit.
    /// </summary>
    [Fact]
    public async Task OutboxEnrollmentFailure_FailsTheCommit_AndKeepsPageActive()
    {
        var graph = await SeedWorkspaceAsync();
        Guid pageId;

        await using (var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor()))
        {
            var page = NewActivePage(graph);
            context.Pages.Add(page);
            await context.SaveChangesAsync();
            pageId = page.Id;
        }

        var failingInterceptor = new DomainEventInterceptor(
            new FixedClock(Now),
            new EventTypeRegistry(),
            ClassificationPolicy.CreateBuilder().Build(),
            DeliveryPolicy.CreateBuilder().Build(),
            new FailingArchivedEventMapper(),
            new IntegrationEventCollector());

        await using (var context = _db.CreateContext(SystemTenant(), failingInterceptor))
        {
            var page = await context.Pages.IgnoreQueryFilters().SingleAsync(p => p.Id == pageId);
            page.Archive(graph.AuthorId, Now);

            var act = () => context.SaveChangesAsync();
            await act.Should().ThrowAsync<InvalidOperationException>(
                "outward enrollment runs inside the SaveChanges chain, so its failure aborts the commit");
        }

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.Pages.IgnoreQueryFilters().SingleAsync(p => p.Id == pageId)).Status
            .Should().Be(PageStatus.Active,
            "a failed enrollment must leave the committed lifecycle state Active");
        (await CountArchivedOutboxAsync(graph.WorkspaceId)).Should().Be(0,
            "no outward fact may exist when enrollment fails inside the commit");
    }

    private sealed class FailingArchivedEventMapper : IIntegrationEventMapper<PageArchivedDomainEvent, Notrelix.Application.Events.Documents.PageArchivedIntegrationEvent>
    {
        public Notrelix.Application.Events.Documents.PageArchivedIntegrationEvent? Map(PageArchivedDomainEvent domainEvent) =>
            throw new InvalidOperationException("synthetic enrollment failure inside the interceptor chain");

        IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
        {
            if (domainEvent is PageArchivedDomainEvent archived)
            {
                Map(archived);
            }

            return [];
        }
    }
}
