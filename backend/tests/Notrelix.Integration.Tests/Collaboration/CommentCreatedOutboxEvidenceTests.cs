using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.EventMappers.Collaboration;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Collaboration.Comments.Events;
using Notrelix.Domain.Collaboration.Mentions.Events;
using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Collaboration;

/// <summary>
/// TAC-DC-005 producer side — the pinned Collaboration comment event chain
/// commits atomically: a valid Comment mutation raises the Collaboration-owned
/// Domain fact, the producer-owned mapper turns it into the registry identity
/// "comment.created" V1 with the authoritative AccountId + WorkspaceId
/// envelope, and the outbox record is written inside the same SaveChanges. A
/// rolled-back mutation leaves no committed outward delivery.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class CommentCreatedOutboxEvidenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public CommentCreatedOutboxEvidenceTests(PostgresTestContainer db)
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

    private sealed record CommentGraph(Guid AccountId, Guid WorkspaceId, Guid AuthorId, Guid ItemId);

    private async Task<CommentGraph> SeedBoardItemStackAsync()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ownerId, "DC Outbox WS", $"dc-{Guid.NewGuid():N}", Now);
        var board = Board.Create(accountId, workspace.Id, ownerId, "DC Outbox Board", null, Now);
        var group = BoardGroup.Create(
            accountId, workspace.Id, board.Id, "Todo",
            Color.Create("#808080"), FractionalIndex.Create("a0"), ownerId, Now);
        var item = BoardItem.CreateRoot(
            accountId, workspace.Id, board.Id, group.Id, "Task",
            FractionalIndex.Create("a0"), ownerId, Now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardItems.Add(item);
        await seed.SaveChangesAsync();

        return new CommentGraph(accountId, workspace.Id, ownerId, item.Id);
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
                    .AddScoped<IIntegrationEventMapper, CommentEventMapper>()
                    .BuildServiceProvider()),
            new IntegrationEventCollector());
    }

    private static Comment NewCommentFact(CommentGraph graph) =>
        Comment.Create(
            graph.AccountId,
            graph.WorkspaceId,
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), graph.ItemId, graph.WorkspaceId),
            "Outbox evidence content",
            graph.AuthorId,
            Now);

    private async Task<MessagingOutboxMessage?> SingleCommentOutboxAsync(Guid workspaceId)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.Set<MessagingOutboxMessage>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(message => message.WorkspaceId == workspaceId
                && message.MessageName == "comment.created");
    }

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    [Fact]
    public async Task CommentCreatedFact_OutboxIntent_CommitsAtomically_WithAuthoritativeEnvelopeAndPayload()
    {
        var graph = await SeedBoardItemStackAsync();

        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        context.Comments.Add(NewCommentFact(graph));
        await context.SaveChangesAsync();

        var outbox = await SingleCommentOutboxAsync(graph.WorkspaceId);
        outbox.Should().NotBeNull("a valid comment mutation must stage exactly one producer-owned outward event");
        outbox!.SchemaVersion.Should().Be(1);
        outbox.AccountId.Should().Be(graph.AccountId);
        outbox.WorkspaceId.Should().Be(graph.WorkspaceId);

        outbox.PayloadJson.RootElement.GetProperty("accountId").GetGuid().Should().Be(graph.AccountId);
        outbox.PayloadJson.RootElement.GetProperty("workspaceId").GetGuid().Should().Be(graph.WorkspaceId);
        outbox.PayloadJson.RootElement.GetProperty("targetType").GetString().Should().Be(
            "work-management.board-item",
            "the producer maps the canonical resource kind string, not the CLR type name");
        outbox.PayloadJson.RootElement.GetProperty("body").GetString().Should().Be(
            "Outbox evidence content",
            "the producer maps the owned comment content fact, not an empty placeholder");
    }

    [Fact]
    public async Task RolledBackCommentFact_NoCommittedOutwardDelivery()
    {
        var graph = await SeedBoardItemStackAsync();

        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Comments.Add(NewCommentFact(graph));
        await context.SaveChangesAsync();
        await transaction.RollbackAsync();

        var outbox = await SingleCommentOutboxAsync(graph.WorkspaceId);
        outbox.Should().BeNull("a rolled-back comment mutation must leave no committed outward delivery");
    }

    /// <summary>
    /// TAC-XPK-009 — ResourceRef→Collaboration flow: the foreign Work target
    /// is referenced by stable identity only. Commenting leaves the target
    /// BoardItem aggregate byte-for-byte unchanged — no Work aggregate
    /// mutation or navigation exists anywhere in the Collaboration path.
    /// </summary>
    [Fact]
    public async Task CommentOnForeignBoardItem_TargetAggregateUnchanged()
    {
        var graph = await SeedBoardItemStackAsync();

        long beforeVersion;
        string beforeName;
        DateTimeOffset? beforeUpdatedAt;
        await using (var snapshot = _db.CreateContext(SystemTenant()))
        {
            var item = await snapshot.BoardItems.AsNoTracking().SingleAsync(i => i.Id == graph.ItemId);
            beforeVersion = item.Version;
            beforeName = item.Name;
            beforeUpdatedAt = item.UpdatedAt;
        }

        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor());
        context.Comments.Add(NewCommentFact(graph));
        await context.SaveChangesAsync();

        await using var verify = _db.CreateContext(SystemTenant());
        var itemAfter = await verify.BoardItems.AsNoTracking().SingleAsync(i => i.Id == graph.ItemId);
        itemAfter.Version.Should().Be(beforeVersion);
        itemAfter.Name.Should().Be(beforeName);
        itemAfter.UpdatedAt.Should().Be(beforeUpdatedAt);
        itemAfter.DomainEvents.Should().BeEmpty("the Work aggregate owns no part of the comment mutation");
    }

    /// <summary>
    /// M7 composite-write atomicity — the comment and its mention entities are
    /// one transaction with their outward enrollments: a mention-enrollment
    /// failure inside the actual interceptor chain fails the whole SaveChanges,
    /// leaving no comment, no mention, and neither outward fact in committed
    /// state. "Same DbContext" is proven atomic, not assumed.
    /// </summary>
    [Fact]
    public async Task MentionEnrollmentFailure_RollsBackCommentMentionAndBothOutboxFacts()
    {
        var graph = await SeedBoardItemStackAsync();
        var mentionedUserId = Guid.NewGuid();

        var failingInterceptor = new DomainEventInterceptor(
            new FixedClock(Now),
            new EventTypeRegistry(),
            ClassificationPolicy.CreateBuilder().Build(),
            DeliveryPolicy.CreateBuilder().Build(),
            new FailingMentionOnlyMapper(),
            new IntegrationEventCollector());

        await using (var context = _db.CreateContext(SystemTenant(), failingInterceptor))
        {
            var comment = Comment.Create(
                graph.AccountId,
                graph.WorkspaceId,
                ResourceRef.Create(ResourceKind.Create("work-management.board-item"), graph.ItemId, graph.WorkspaceId),
                "Composite content",
                graph.AuthorId,
                Now);
            context.Comments.Add(comment);
            context.PageMentions.Add(Domain.Collaboration.Mentions.Mention.Create(
                graph.AccountId,
                graph.WorkspaceId,
                ResourceRef.Create(ResourceKind.Create("work-management.board-item"), graph.ItemId, graph.WorkspaceId),
                Domain.Collaboration.Mentions.MentionType.User,
                mentionedUserId,
                graph.AuthorId,
                Now));

            var act = () => context.SaveChangesAsync();
            await act.Should().ThrowAsync<InvalidOperationException>(
                "mention enrollment runs inside the SaveChanges chain, so its failure aborts the whole composite write");
        }

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.Comments.IgnoreQueryFilters()
            .AnyAsync(c => c.WorkspaceId == graph.WorkspaceId)).Should().BeFalse(
            "the comment must not survive the aborted composite write");
        (await verify.PageMentions.IgnoreQueryFilters()
            .AnyAsync(m => m.WorkspaceId == graph.WorkspaceId)).Should().BeFalse(
            "the mention must not survive the aborted composite write");
        (await verify.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
            .CountAsync(m => m.WorkspaceId == graph.WorkspaceId
                && (m.MessageName == "comment.created" || m.MessageName == "mention.created"))).Should().Be(0,
            "neither outward fact may survive the aborted composite write");
    }

    private sealed class FailingMentionOnlyMapper :
        IIntegrationEventMapper<CommentCreatedDomainEvent, Application.Events.Collaboration.CommentCreatedIntegrationEvent>,
        IIntegrationEventMapper<MentionCreatedDomainEvent, Application.Events.Collaboration.MentionCreatedIntegrationEvent>
    {
        public Application.Events.Collaboration.CommentCreatedIntegrationEvent? Map(CommentCreatedDomainEvent domainEvent)
        {
            var workspaceId = domainEvent.WorkspaceId;
            return new Application.Events.Collaboration.CommentCreatedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                AccountId: domainEvent.AccountId,
                CommentId: domainEvent.CommentId,
                WorkspaceId: workspaceId,
                TargetType: domainEvent.Target.Kind.Value,
                TargetId: domainEvent.Target.ResourceId,
                AuthorId: domainEvent.CreatedBy,
                Body: domainEvent.Content,
                CorrelationId: domainEvent.EventId,
                OccurredAt: domainEvent.OccurredAt);
        }

        public Application.Events.Collaboration.MentionCreatedIntegrationEvent? Map(MentionCreatedDomainEvent domainEvent) =>
            throw new InvalidOperationException("synthetic mention enrollment failure inside the interceptor chain");

        IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
        {
            if (domainEvent is CommentCreatedDomainEvent created)
            {
                var mapped = Map(created);
                if (mapped is not null) return [new IntegrationEventMapping(mapped)];
            }

            if (domainEvent is MentionCreatedDomainEvent mentioned)
            {
                Map(mentioned);
            }

            return [];
        }
    }
}
