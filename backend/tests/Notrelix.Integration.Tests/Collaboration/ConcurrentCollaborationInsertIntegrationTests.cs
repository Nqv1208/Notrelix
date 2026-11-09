using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Events.Collaboration;
using Notrelix.Application.Features.Collaboration.Reactions.Commands.AddReaction;
using Notrelix.Application.Features.Collaboration.Watchers.Commands.FollowBoardItem;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Infrastructure.Data.Collaboration;
using Notrelix.Infrastructure.Messaging.Consumers.Collaboration;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Collaboration;

/// <summary>
/// UC-COL-A09, UC-COL-H03 and UC-COL-H06 under real concurrency against PostgreSQL. Several requests race on the same
/// logical insert, each in its own transaction and context. The outcome must be one stored row and no failed request:
/// a racing reaction add or follow is a no-op (the transaction-scoped lock plus the existing unique index), and a
/// racing mention-notification delivery is a no-op (the unique deduplication key). Runs in CI; needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ConcurrentCollaborationInsertIntegrationTests : IAsyncLifetime
{
    private const int Racers = 8;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public ConcurrentCollaborationInsertIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ConcurrentAddsOfOneReaction_StoreOneRow_AndEveryAddSucceeds()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var comment = Comment.Create(
            accountId,
            workspaceId,
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), itemId, workspaceId),
            "race target",
            userId,
            DateTimeOffset.UtcNow);

        await using (var seed = _db.CreateOwnerContext(SystemTenant()))
        {
            seed.Comments.Add(comment);
            await seed.SaveChangesAsync();
        }

        var results = await Task.WhenAll(Enumerable.Range(0, Racers)
            .Select(_ => AddReactionAsync(accountId, workspaceId, userId, itemId, comment.Id, "👍")));

        results.Should().HaveCount(Racers);
        results.Should().OnlyContain(result => result.Succeeded, "a racing duplicate is a no-op, never a failure");

        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var stored = await probe.Reactions.CountAsync(r =>
            r.UserId == userId && r.Target.ResourceId == comment.Id && r.Emoji.Code == "👍");
        stored.Should().Be(1, "one person holds one reaction per emoji and comment");
    }

    [Fact]
    public async Task ConcurrentDeliveriesOfOneMentionCreated_StoreOneNotification_AndNoDeliveryFails()
    {
        var message = new MentionCreatedIntegrationEvent(
            EventId: Guid.NewGuid(),
            AccountId: Guid.NewGuid(),
            MentionId: Guid.NewGuid(),
            WorkspaceId: Guid.NewGuid(),
            TargetType: "collaboration.comment",
            TargetId: Guid.NewGuid(),
            MentionedUserId: Guid.NewGuid(),
            MentionedByUserId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            Excerpt: "race");

        // Any delivery that fails would fault the WhenAll; a duplicate delivery must complete as a success.
        await Task.WhenAll(Enumerable.Range(0, Racers).Select(_ => DeliverAsync(message)));

        var deduplicationKey = $"mention-created:{message.MentionId}:{message.MentionedUserId}";
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var stored = await probe.NotificationItems.CountAsync(n => n.DeduplicationKey == deduplicationKey);
        stored.Should().Be(1, "one notification per (mention, person)");
    }

    [Fact]
    public async Task ConcurrentFollowsOfOneItem_StoreOneWatcher_AndEveryFollowSucceeds()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var results = await Task.WhenAll(Enumerable.Range(0, Racers)
            .Select(_ => FollowAsync(accountId, workspaceId, userId, itemId)));

        results.Should().HaveCount(Racers);
        results.Should().OnlyContain(result => result.Succeeded, "a racing duplicate follow is a no-op, never a failure");

        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var stored = await probe.ResourceWatchers.CountAsync(w => w.UserId == userId && w.Target.ResourceId == itemId);
        stored.Should().Be(1, "one person follows one item once");
    }

    private async Task<Result> FollowAsync(Guid accountId, Guid workspaceId, Guid userId, Guid itemId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var request = new Mock<ICurrentRequestContext>();
        request.Setup(r => r.UserId).Returns(userId);
        request.Setup(r => r.RequireAccountId()).Returns(accountId);
        request.Setup(r => r.RequireWorkspaceId()).Returns(workspaceId);

        await using var transaction = await context.Database.BeginTransactionAsync();
        var handler = new FollowBoardItemCommandHandler(
            context,
            request.Object,
            FakeDateTimeProvider.WithFixedTime(DateTimeOffset.UtcNow),
            new PostgresWatcherWriteLock(context));

        var result = await handler.Handle(new FollowBoardItemCommand(itemId), CancellationToken.None);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    private async Task<Result> AddReactionAsync(
        Guid accountId, Guid workspaceId, Guid userId, Guid itemId, Guid commentId, string emoji)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var request = new Mock<ICurrentRequestContext>();
        request.Setup(r => r.UserId).Returns(userId);
        request.Setup(r => r.RequireAccountId()).Returns(accountId);
        request.Setup(r => r.RequireWorkspaceId()).Returns(workspaceId);

        await using var transaction = await context.Database.BeginTransactionAsync();
        var handler = new AddReactionCommandHandler(
            context,
            request.Object,
            FakeDateTimeProvider.WithFixedTime(DateTimeOffset.UtcNow),
            new PostgresReactionWriteLock(context));

        var result = await handler.Handle(new AddReactionCommand(itemId, commentId, emoji), CancellationToken.None);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    private async Task DeliverAsync(MentionCreatedIntegrationEvent message)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var consumer = new MentionCreatedNotificationConsumer(context, NullLogger<MentionCreatedNotificationConsumer>.Instance);

        var consumeContext = new Mock<ConsumeContext<MentionCreatedIntegrationEvent>>();
        consumeContext.SetupGet(c => c.Message).Returns(message);
        consumeContext.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(consumeContext.Object);
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}
