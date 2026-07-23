using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Domain.Integrations.Rules;

public static class IntegrationRules
{
    public static void EnsureCanReconnect(IntegrationConnectionStatus currentStatus)
    {
        if (currentStatus == IntegrationConnectionStatus.Active)
            throw new BusinessRuleException(BusinessRuleCodes.Integrations_Connection_AlreadyActive, "Connection is already active.");
    }

    public static void EnsureExpirationInFuture(DateTimeOffset expiresAt, DateTimeOffset now)
    {
        if (expiresAt <= now)
            throw new BusinessRuleException(BusinessRuleCodes.Integrations_Connection_ExpirationMustBeFuture, "Connection expiration must be in the future.");
    }
}
