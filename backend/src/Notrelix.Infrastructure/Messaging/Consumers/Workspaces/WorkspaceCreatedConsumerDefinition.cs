namespace Notrelix.Infrastructure.Messaging.Consumers.Workspaces;

public sealed class WorkspaceCreatedConsumerDefinition : ConsumerDefinition<WorkspaceCreatedConsumer>
{
    public WorkspaceCreatedConsumerDefinition()
    {
        EndpointName = "notrelix-workspace-created-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WorkspaceCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
            r.Ignore<ArgumentException>();
        });
    }
}
