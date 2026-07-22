namespace Notrelix.Infrastructure.Messaging.Consumers.Workspaces;

public sealed class WorkspaceArchivedConsumerDefinition : ConsumerDefinition<WorkspaceArchivedConsumer>
{
    public WorkspaceArchivedConsumerDefinition()
    {
        EndpointName = "notrelix-workspace-archived-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WorkspaceArchivedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class WorkspaceUnarchivedConsumerDefinition : ConsumerDefinition<WorkspaceUnarchivedConsumer>
{
    public WorkspaceUnarchivedConsumerDefinition()
    {
        EndpointName = "notrelix-workspace-unarchived-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WorkspaceUnarchivedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class TeamCreatedConsumerDefinition : ConsumerDefinition<TeamCreatedConsumer>
{
    public TeamCreatedConsumerDefinition()
    {
        EndpointName = "notrelix-team-created-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<TeamCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class SpaceCreatedConsumerDefinition : ConsumerDefinition<SpaceCreatedConsumer>
{
    public SpaceCreatedConsumerDefinition()
    {
        EndpointName = "notrelix-space-created-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<SpaceCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}
