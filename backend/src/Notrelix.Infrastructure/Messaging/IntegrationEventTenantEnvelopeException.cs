namespace Notrelix.Infrastructure.Messaging;

public sealed class IntegrationEventTenantEnvelopeException : ArgumentException
{
    public IntegrationEventTenantEnvelopeException(string message)
        : base(message)
    {
    }
}
