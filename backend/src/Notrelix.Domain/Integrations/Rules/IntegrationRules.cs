using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Domain.Integrations.Rules;

public static class IntegrationRules
{
    public static void EnsureCanReconnect(IntegrationConnectionStatus currentStatus)
    {
        if (currentStatus == IntegrationConnectionStatus.Active)
            throw new BusinessRuleException("Connection is already active.");
    }

    public static void EnsureExpirationInFuture(DateTimeOffset expiresAt, DateTimeOffset now)
    {
        if (expiresAt <= now)
            throw new BusinessRuleException("Connection expiration must be in the future.");
    }
}
