using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

public sealed class BoardItemCreatedFollowConsumerDefinition : ConsumerDefinition<BoardItemCreatedFollowConsumer>
{
    public BoardItemCreatedFollowConsumerDefinition()
    {
        EndpointName = "notrelix-collab-item-created-follow-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemCreatedFollowConsumer> consumerConfigurator,
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

public sealed class BoardItemMemberAssignedFollowConsumerDefinition : ConsumerDefinition<BoardItemMemberAssignedFollowConsumer>
{
    public BoardItemMemberAssignedFollowConsumerDefinition()
    {
        EndpointName = "notrelix-collab-item-assigned-follow-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemMemberAssignedFollowConsumer> consumerConfigurator,
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

public sealed class BoardItemStatusChangedFollowConsumerDefinition : ConsumerDefinition<BoardItemStatusChangedFollowConsumer>
{
    public BoardItemStatusChangedFollowConsumerDefinition()
    {
        EndpointName = "notrelix-collab-item-status-follow-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemStatusChangedFollowConsumer> consumerConfigurator,
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

public sealed class CommentCreatedFollowConsumerDefinition : ConsumerDefinition<CommentCreatedFollowConsumer>
{
    public CommentCreatedFollowConsumerDefinition()
    {
        EndpointName = "notrelix-collab-comment-follow-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CommentCreatedFollowConsumer> consumerConfigurator,
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

public sealed class BoardItemOwnersRemindedNotificationConsumerDefinition : ConsumerDefinition<BoardItemOwnersRemindedNotificationConsumer>
{
    public BoardItemOwnersRemindedNotificationConsumerDefinition()
    {
        EndpointName = "notrelix-collab-item-owners-reminded-notification-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemOwnersRemindedNotificationConsumer> consumerConfigurator,
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
