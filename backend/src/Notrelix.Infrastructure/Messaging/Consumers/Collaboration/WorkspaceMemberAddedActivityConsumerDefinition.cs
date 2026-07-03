using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

public sealed class WorkspaceMemberAddedActivityConsumerDefinition : ConsumerDefinition<WorkspaceMemberAddedActivityConsumer>
{
    public WorkspaceMemberAddedActivityConsumerDefinition()
    {
        EndpointName = "notrelix-activity-member-added-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WorkspaceMemberAddedActivityConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 8;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
            r.Ignore<ArgumentException>();
            r.Ignore<DomainException>();
        });
    }
}
