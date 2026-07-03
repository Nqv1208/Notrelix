namespace Notrelix.Infrastructure.Messaging.Consumers.Workspaces;

public sealed class WorkspaceMemberAddedConsumerDefinition : ConsumerDefinition<WorkspaceMemberAddedConsumer>
{
    public WorkspaceMemberAddedConsumerDefinition()
    {
        EndpointName = "notrelix-workspace-member-added-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WorkspaceMemberAddedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 8;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
            r.Ignore<ArgumentException>();
        });
    }
}
