using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.UserRegistered;

public sealed class WorkspaceProvisioningConsumerDefinition : ConsumerDefinition<WorkspaceProvisioningConsumer>
{
    public WorkspaceProvisioningConsumerDefinition()
    {
        EndpointName = "notrelix-identity-workspace-provision-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WorkspaceProvisioningConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 4;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
            r.Ignore<ArgumentException>();
            r.Ignore<DomainException>();
        });
    }
}
