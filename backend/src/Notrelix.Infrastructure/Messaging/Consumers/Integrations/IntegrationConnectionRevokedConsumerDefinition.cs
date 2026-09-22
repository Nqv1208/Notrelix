namespace Notrelix.Infrastructure.Messaging.Consumers.Integrations;

public sealed class IntegrationConnectionRevokedConsumerDefinition
    : ConsumerDefinition<IntegrationConnectionRevokedConsumer>
{
    public IntegrationConnectionRevokedConsumerDefinition()
    {
        EndpointName = "notrelix-integrations-connection-revoked-v1";
    }
}
