namespace Notrelix.Infrastructure.Messaging.Consumers.Automation;

public sealed class BoardItemMemberAssignedAutomationConsumerDefinition
    : ConsumerDefinition<BoardItemMemberAssignedAutomationConsumer>
{
    public BoardItemMemberAssignedAutomationConsumerDefinition()
    {
        EndpointName = "notrelix-automation-board-item-member-assigned-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemMemberAssignedAutomationConsumer> consumerConfigurator,
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

public sealed class BoardItemMovedAutomationConsumerDefinition
    : ConsumerDefinition<BoardItemMovedAutomationConsumer>
{
    public BoardItemMovedAutomationConsumerDefinition()
    {
        EndpointName = "notrelix-automation-board-item-moved-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemMovedAutomationConsumer> consumerConfigurator,
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

public sealed class BoardItemCreatedAutomationConsumerDefinition
    : ConsumerDefinition<BoardItemCreatedAutomationConsumer>
{
    public BoardItemCreatedAutomationConsumerDefinition()
    {
        EndpointName = "notrelix-automation-board-item-created-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemCreatedAutomationConsumer> consumerConfigurator,
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

public sealed class AutomationMoveItemDispatchConsumerDefinition
    : ConsumerDefinition<AutomationMoveItemDispatchConsumer>
{
    public AutomationMoveItemDispatchConsumerDefinition()
    {
        EndpointName = "notrelix-automation-move-item-dispatch-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<AutomationMoveItemDispatchConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 8;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class N8nDispatchConsumerDefinition
    : ConsumerDefinition<N8nDispatchConsumer>
{
    public N8nDispatchConsumerDefinition()
    {
        EndpointName = "notrelix-automation-n8n-dispatch-v1";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<N8nDispatchConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 8;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}
