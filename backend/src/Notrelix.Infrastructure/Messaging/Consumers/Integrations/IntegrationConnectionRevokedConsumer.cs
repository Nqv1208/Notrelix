using Notrelix.Application.Events.Integrations;
using Notrelix.Application.Features.Integrations.Public.Secrets;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Messaging.Consumers.Integrations;

/// <summary>
/// CAL-CONN-001 post-commit cleanup. The connection/binding lifecycle is
/// already durable before this consumer runs. Physical secret cleanup is a
/// separate provider-neutral effect with broker-owned retry and dead-letter
/// handling; it never participates in the local aggregate transaction.
/// </summary>
public sealed class IntegrationConnectionRevokedConsumer
    : IConsumer<IntegrationConnectionRevokedIntegrationEvent>
{
    private readonly ApplicationDbContext _db;
    private readonly IIntegrationSecretStore _secretStore;
    private readonly ILogger<IntegrationConnectionRevokedConsumer> _logger;

    public IntegrationConnectionRevokedConsumer(
        ApplicationDbContext db,
        IIntegrationSecretStore secretStore,
        ILogger<IntegrationConnectionRevokedConsumer> logger)
    {
        _db = db;
        _secretStore = secretStore;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IntegrationConnectionRevokedIntegrationEvent> context)
    {
        var message = context.Message;
        var secretVersions = await _db.IntegrationSecretVersions
            .Where(version => version.ConnectionId == message.ConnectionId)
            .Distinct()
            .ToListAsync(context.CancellationToken);
        var references = secretVersions
            .Select(version => version.SecretReference.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (references.Count == 0)
        {
            _logger.LogInformation(
                "No physical secrets require cleanup for revoked integration connection {ConnectionId}",
                message.ConnectionId);
            return;
        }

        foreach (var reference in references)
        {
            try
            {
                var result = await _secretStore.RevokeAsync(reference, context.CancellationToken);
                switch (result.Outcome)
                {
                    case ProviderCleanupOutcome.Success:
                        break;
                    case ProviderCleanupOutcome.Retryable:
                        throw new IntegrationSecretCleanupRetryableException(
                            message.ConnectionId,
                            reference,
                            result.Detail ?? "provider cleanup requested a retry");
                    case ProviderCleanupOutcome.Unknown:
                        throw new IntegrationSecretCleanupUnknownException(
                            message.ConnectionId,
                            reference,
                            result.Detail ?? "provider cleanup outcome is unknown");
                    case ProviderCleanupOutcome.Terminal:
                        throw new IntegrationSecretCleanupTerminalException(
                            message.ConnectionId,
                            reference,
                            result.Detail ?? "provider cleanup failed terminally");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (IntegrationSecretCleanupRetryableException)
            {
                throw;
            }
            catch (IntegrationSecretCleanupUnknownException)
            {
                throw;
            }
            catch (IntegrationSecretCleanupTerminalException)
            {
                throw;
            }
            catch (ArgumentException ex)
            {
                throw new IntegrationSecretCleanupTerminalException(
                    message.ConnectionId,
                    reference,
                    ex.Message,
                    ex);
            }
            catch (TimeoutException ex)
            {
                throw new IntegrationSecretCleanupUnknownException(
                    message.ConnectionId,
                    reference,
                    ex.Message,
                    ex);
            }
            catch (Exception ex)
            {
                throw new IntegrationSecretCleanupRetryableException(
                    message.ConnectionId,
                    reference,
                    ex.Message,
                    ex);
            }
        }

        _logger.LogInformation(
            "Physical secret cleanup completed for revoked integration connection {ConnectionId}",
            message.ConnectionId);
    }
}

public sealed class IntegrationSecretCleanupRetryableException : Exception
{
    public IntegrationSecretCleanupRetryableException(Guid connectionId, string reference, string detail, Exception? inner = null)
        : base($"Retryable secret cleanup failed for connection {connectionId}, reference {reference}: {detail}", inner)
    {
    }
}

public sealed class IntegrationSecretCleanupUnknownException : Exception
{
    public IntegrationSecretCleanupUnknownException(Guid connectionId, string reference, string detail, Exception? inner = null)
        : base($"Unknown secret cleanup outcome for connection {connectionId}, reference {reference}: {detail}", inner)
    {
    }
}

public sealed class IntegrationSecretCleanupTerminalException : ArgumentException
{
    public IntegrationSecretCleanupTerminalException(Guid connectionId, string reference, string detail, Exception? inner = null)
        : base($"Terminal secret cleanup failure for connection {connectionId}, reference {reference}: {detail}", inner)
    {
    }
}
