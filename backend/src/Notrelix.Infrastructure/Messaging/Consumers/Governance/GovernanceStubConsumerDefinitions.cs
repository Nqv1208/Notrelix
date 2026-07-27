namespace Notrelix.Infrastructure.Messaging.Consumers.Governance;

public sealed class CustomRoleAssignedConsumerDefinition : ConsumerDefinition<CustomRoleAssignedConsumer>
{
    public CustomRoleAssignedConsumerDefinition()
    {
        EndpointName = "notrelix-governance-role-assigned-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CustomRoleAssignedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class ResourcePermissionGrantedConsumerDefinition : ConsumerDefinition<ResourcePermissionGrantedConsumer>
{
    public ResourcePermissionGrantedConsumerDefinition()
    {
        EndpointName = "notrelix-governance-permission-granted-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ResourcePermissionGrantedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class ResourcePermissionRevokedConsumerDefinition : ConsumerDefinition<ResourcePermissionRevokedConsumer>
{
    public ResourcePermissionRevokedConsumerDefinition()
    {
        EndpointName = "notrelix-governance-permission-revoked-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ResourcePermissionRevokedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}
