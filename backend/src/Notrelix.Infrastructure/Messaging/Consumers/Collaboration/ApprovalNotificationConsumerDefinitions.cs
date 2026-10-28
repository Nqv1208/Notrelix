using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

public sealed class ApprovalRequestedNotificationConsumerDefinition : ConsumerDefinition<ApprovalRequestedNotificationConsumer>
{
    public ApprovalRequestedNotificationConsumerDefinition()
    {
        EndpointName = "notrelix-collab-approval-requested-notification-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ApprovalRequestedNotificationConsumer> consumerConfigurator,
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

public sealed class ApprovalDecidedNotificationConsumerDefinition : ConsumerDefinition<ApprovalDecidedNotificationConsumer>
{
    public ApprovalDecidedNotificationConsumerDefinition()
    {
        EndpointName = "notrelix-collab-approval-decided-notification-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ApprovalDecidedNotificationConsumer> consumerConfigurator,
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
