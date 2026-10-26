using System.Text.Json;
using Notrelix.Application.Events.Integrations;
using Notrelix.Application.Features.Integrations.Public.Secrets;
using Notrelix.Domain.Integrations.Connections;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Integrations.Connections;

namespace Notrelix.Infrastructure.Messaging.Consumers.Integrations;

/// <summary>
/// Delivers one signed message to an outgoing webhook connection and records the outcome on it. The receive
/// pipeline has already restored the workspace tenant, so the connection and its secret are read under RLS.
///
/// <list type="bullet">
///   <item>2xx: the connection records a success (a failing connection is healthy again).</item>
///   <item>5xx, 408, 429, timeout, refused: the message is redelivered under the same delivery identity
///   (`X-Notrelix-Delivery`), so the receiver can ignore a repeat; once the attempts are used up the
///   connection records the failure and the message is done.</item>
///   <item>Any other answer or a refused address: the failure is recorded at once and the message is done.</item>
/// </list>
///
/// A revoked or deleted connection is a converged no-op; a paused one receives only a test delivery.
/// </summary>
[OrderingNotApplicable("Each message is one independent delivery whose outcome is recorded on the connection; the receiver orders by the delivery identity.")]
public sealed class IntegrationWebhookDispatchRequestedConsumer : IConsumer<IntegrationWebhookDispatchRequestedV1>
{
    /// <summary>Redeliveries after the first attempt, matching the consumer definition.</summary>
    public const int MaxRedeliveries = 3;

    private readonly ApplicationDbContext _db;
    private readonly IIntegrationSecretStore _secrets;
    private readonly OutboundWebhookSender _sender;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<IntegrationWebhookDispatchRequestedConsumer> _logger;

    public IntegrationWebhookDispatchRequestedConsumer(
        ApplicationDbContext db,
        IIntegrationSecretStore secrets,
        OutboundWebhookSender sender,
        IDateTimeProvider clock,
        ILogger<IntegrationWebhookDispatchRequestedConsumer> logger)
    {
        _db = db;
        _secrets = secrets;
        _sender = sender;
        _clock = clock;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IntegrationWebhookDispatchRequestedV1> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var connection = await _db.IntegrationConnections.FirstOrDefaultAsync(c =>
            c.Id == message.ConnectionId
            && c.WorkspaceId == message.WorkspaceIdValue
            && c.Provider == IntegrationProvider.Webhook, ct);

        var isTest = message.DeliveryType == IntegrationWebhookDispatchRequestedV1.TestType;
        if (connection is null
            || connection.IsDeleted
            || connection.Status == IntegrationConnectionStatus.Revoked
            || connection.TargetUrl is null
            || (connection.Status == IntegrationConnectionStatus.Paused && !isTest))
        {
            _logger.LogDebug("Webhook delivery {DeliveryId} has no active connection; nothing to deliver", message.DeliveryId);
            return;
        }

        var secretReference = await _db.IntegrationSecretVersions
            .Where(v => v.ConnectionId == connection.Id && v.Version == connection.CurrentSecretVersion)
            .Select(v => v.SecretReference)
            .FirstOrDefaultAsync(ct);
        var secret = secretReference is null ? null : await _secrets.ReadAsync(secretReference.Value, ct);
        if (secret is null)
        {
            connection.RecordFailure("The signing secret is missing.", _clock.UtcNow);
            await _db.SaveChangesAsync(ct);
            return;
        }

        var now = _clock.UtcNow;
        var body = JsonSerializer.Serialize(new
        {
            id = message.DeliveryId,
            type = message.DeliveryType,
            createdAt = message.OccurredAt,
            workspaceId = message.WorkspaceIdValue,
            data = ParseData(message.PayloadJson),
        });

        var result = await _sender.SendAsync(
            connection.TargetUrl, secret, message.DeliveryId, message.DeliveryType, body, now, ct);

        switch (result.Outcome)
        {
            case WebhookSendOutcome.Delivered:
                connection.RecordSuccess(_clock.UtcNow);
                break;

            case WebhookSendOutcome.Transient when context.GetRetryAttempt() < MaxRedeliveries:
                throw new WebhookDispatchRetryableException(message.DeliveryId, result.Description);

            default:
                connection.RecordFailure(result.Description, _clock.UtcNow);
                break;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static JsonElement ParseData(string? payloadJson)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson);
        return document.RootElement.Clone();
    }
}

/// <summary>Asks the delivery mechanism to redeliver the same delivery identity.</summary>
public sealed class WebhookDispatchRetryableException(Guid deliveryId, string description)
    : InvalidOperationException($"webhook delivery {deliveryId} will be retried: {description}");
