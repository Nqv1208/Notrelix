using Notrelix.Application.Events.Collaboration;
using Notrelix.Application.Events.Integrations;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;

namespace Notrelix.Infrastructure.Messaging.Consumers.Integrations;

[OrderingNotApplicable("Each fact is delivered independently under a delivery identity derived from the fact and the connection; deliveries never supersede each other.")]
public sealed class BoardItemCreatedWebhookFanOutConsumer : IConsumer<BoardItemCreatedIntegrationEventV2>
{
    private readonly WebhookEventFanOut _fanOut;

    public BoardItemCreatedWebhookFanOutConsumer(WebhookEventFanOut fanOut) => _fanOut = fanOut;

    public async Task Consume(ConsumeContext<BoardItemCreatedIntegrationEventV2> context)
    {
        var msg = context.Message;
        if (msg.AccountId is not { } accountId || accountId == Guid.Empty || msg.WorkspaceId is not { } workspaceId) return;

        await _fanOut.FanOutAsync(
            accountId, workspaceId, msg.EventId, IntegrationWebhookDispatchRequestedV1.ItemCreatedType,
            new { itemId = msg.ItemId, boardId = msg.BoardId, title = msg.Title, actorUserId = msg.ActorUserId },
            msg.ActorUserId, msg.CorrelationId, msg.OccurredAt, context.CancellationToken);
    }
}

[OrderingNotApplicable("Each fact is delivered independently under a delivery identity derived from the fact and the connection; deliveries never supersede each other.")]
public sealed class BoardItemRenamedWebhookFanOutConsumer : IConsumer<BoardItemRenamedIntegrationEvent>
{
    private readonly WebhookEventFanOut _fanOut;

    public BoardItemRenamedWebhookFanOutConsumer(WebhookEventFanOut fanOut) => _fanOut = fanOut;

    public async Task Consume(ConsumeContext<BoardItemRenamedIntegrationEvent> context)
    {
        var msg = context.Message;
        if (msg.AccountId is not { } accountId || accountId == Guid.Empty || msg.WorkspaceId is not { } workspaceId) return;

        await _fanOut.FanOutAsync(
            accountId, workspaceId, msg.EventId, IntegrationWebhookDispatchRequestedV1.ItemUpdatedType,
            new { itemId = msg.ItemId, boardId = msg.BoardId, title = msg.NewName, change = "title", actorUserId = msg.ActorUserId },
            msg.ActorUserId, msg.CorrelationId, msg.OccurredAt, context.CancellationToken);
    }
}

[OrderingNotApplicable("Each fact is delivered independently under a delivery identity derived from the fact and the connection; deliveries never supersede each other.")]
public sealed class BoardItemFieldChangedWebhookFanOutConsumer : IConsumer<BoardItemFieldValueChangedIntegrationEvent>
{
    private readonly WebhookEventFanOut _fanOut;
    private readonly IWorkItemNotificationFacts _work;

    public BoardItemFieldChangedWebhookFanOutConsumer(WebhookEventFanOut fanOut, IWorkItemNotificationFacts work)
    {
        _fanOut = fanOut;
        _work = work;
    }

    public async Task Consume(ConsumeContext<BoardItemFieldValueChangedIntegrationEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        if (msg.AccountId is not { } accountId || accountId == Guid.Empty || msg.WorkspaceId is not { } workspaceId) return;

        var facts = await _work.FindAsync(workspaceId, msg.ItemId, msg.FieldId, ct);
        if (facts is null) return;

        if (facts.ChangedFieldIsStatus)
        {
            await _fanOut.FanOutAsync(
                accountId, workspaceId, msg.EventId, IntegrationWebhookDispatchRequestedV1.ItemStatusChangedType,
                new { itemId = msg.ItemId, boardId = msg.BoardId, title = facts.Title, status = facts.StatusName, actorUserId = msg.ActorUserId },
                msg.ActorUserId, msg.CorrelationId, msg.OccurredAt, ct);
            return;
        }

        await _fanOut.FanOutAsync(
            accountId, workspaceId, msg.EventId, IntegrationWebhookDispatchRequestedV1.ItemUpdatedType,
            new { itemId = msg.ItemId, boardId = msg.BoardId, title = facts.Title, change = "field", fieldId = msg.FieldId, actorUserId = msg.ActorUserId },
            msg.ActorUserId, msg.CorrelationId, msg.OccurredAt, ct);
    }
}

[OrderingNotApplicable("Each fact is delivered independently under a delivery identity derived from the fact and the connection; deliveries never supersede each other.")]
public sealed class CommentCreatedWebhookFanOutConsumer : IConsumer<CommentCreatedIntegrationEvent>
{
    private const string BoardItemKind = "work-management.board-item";
    private const int ExcerptLength = 200;

    private readonly WebhookEventFanOut _fanOut;
    private readonly IWorkItemNotificationFacts _work;

    public CommentCreatedWebhookFanOutConsumer(WebhookEventFanOut fanOut, IWorkItemNotificationFacts work)
    {
        _fanOut = fanOut;
        _work = work;
    }

    public async Task Consume(ConsumeContext<CommentCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        if (msg.TargetType != BoardItemKind
            || msg.AccountId is not { } accountId || accountId == Guid.Empty || msg.WorkspaceId is not { } workspaceId) return;

        var facts = await _work.FindAsync(workspaceId, msg.TargetId, null, ct);
        if (facts is null) return;

        var excerpt = msg.Body.Length <= ExcerptLength ? msg.Body : msg.Body[..ExcerptLength];
        await _fanOut.FanOutAsync(
            accountId, workspaceId, msg.EventId, IntegrationWebhookDispatchRequestedV1.CommentAddedType,
            new { itemId = msg.TargetId, boardId = facts.BoardId, title = facts.Title, commentId = msg.CommentId, authorId = msg.AuthorId, excerpt },
            msg.AuthorId, msg.CorrelationId, msg.OccurredAt, ct);
    }
}
