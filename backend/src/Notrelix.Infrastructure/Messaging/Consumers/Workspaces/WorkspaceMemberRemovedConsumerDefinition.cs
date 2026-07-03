namespace Notrelix.Infrastructure.Messaging.Consumers.Workspaces;

public sealed class WorkspaceMemberRemovedConsumerDefinition : ConsumerDefinition<WorkspaceMemberRemovedConsumer>
{
    public WorkspaceMemberRemovedConsumerDefinition()
    {
        EndpointName = "notrelix-workspace-member-removed-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WorkspaceMemberRemovedConsumer> consumerConfigurator,
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
