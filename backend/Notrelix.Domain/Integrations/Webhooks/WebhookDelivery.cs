using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Integrations.Webhooks.Events;

namespace Notrelix.Domain.Integrations.Webhooks;

public class WebhookDelivery : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid WebhookSubscriptionId { get; private set; }
    public WebhookEventType EventType { get; private set; }
    public JsonValue Payload { get; private set; } = null!;
    public WebhookDeliveryStatus Status { get; private set; }
    public int? ResponseStatusCode { get; private set; }
    public string? ResponseBody { get; private set; }
    public int RetryCount { get; private set; }
    public DateTimeOffset? NextRetryAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public int MaxRetries { get; private set; } = 3;

    private WebhookDelivery() : base() { }

    public static WebhookDelivery Create(Guid workspaceId, Guid subscriptionId, WebhookEventType eventType, JsonValue payload, DateTimeOffset createdAt, int maxRetries = 3)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(subscriptionId);
        Guard.NotNull(payload);

        var delivery = new WebhookDelivery
        {
            WorkspaceId = workspaceId,
            WebhookSubscriptionId = subscriptionId,
            EventType = eventType,
            Payload = payload,
            Status = WebhookDeliveryStatus.Pending,
            MaxRetries = maxRetries
        };

        delivery.AddDomainEvent(new WebhookDeliveryRecordedDomainEvent(workspaceId, subscriptionId, delivery.Id, WebhookDeliveryStatus.Pending, createdAt));
        return delivery;
    }

    public void MarkDelivered(int statusCode, string? responseBody, DateTimeOffset deliveredAt)
    {
        if (Status != WebhookDeliveryStatus.Pending && Status != WebhookDeliveryStatus.Retrying)
            throw new BusinessRuleException($"Cannot mark delivery as sent from status {Status}.");

        Status = WebhookDeliveryStatus.Sent;
        ResponseStatusCode = statusCode;
        ResponseBody = responseBody;
        DeliveredAt = deliveredAt;
        IncrementVersion();
        AddDomainEvent(new WebhookDeliveryRecordedDomainEvent(WorkspaceId, WebhookSubscriptionId, Id, WebhookDeliveryStatus.Sent, deliveredAt));
    }

    public void MarkFailed(int? statusCode, string? responseBody, DateTimeOffset failedAt, string? reason = null)
    {
        if (Status != WebhookDeliveryStatus.Pending && Status != WebhookDeliveryStatus.Retrying)
            throw new BusinessRuleException($"Cannot mark delivery as failed from status {Status}.");

        Status = WebhookDeliveryStatus.Failed;
        ResponseStatusCode = statusCode;
        ResponseBody = responseBody;
        FailedAt = failedAt;
        FailureReason = reason;
        IncrementVersion();
        AddDomainEvent(new WebhookDeliveryRecordedDomainEvent(WorkspaceId, WebhookSubscriptionId, Id, WebhookDeliveryStatus.Failed, failedAt));
    }

    public void ScheduleRetry(DateTimeOffset nextRetryAt)
    {
        if (Status != WebhookDeliveryStatus.Failed)
            throw new BusinessRuleException("Can only schedule retry for a failed delivery.");

        if (RetryCount >= MaxRetries)
            throw new BusinessRuleException($"Maximum retry count ({MaxRetries}) reached.");

        Status = WebhookDeliveryStatus.Retrying;
        RetryCount++;
        NextRetryAt = nextRetryAt;
        IncrementVersion();
    }
}
