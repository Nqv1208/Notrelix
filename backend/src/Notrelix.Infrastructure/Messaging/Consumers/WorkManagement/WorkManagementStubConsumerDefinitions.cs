namespace Notrelix.Infrastructure.Messaging.Consumers.WorkManagement;

public sealed class BoardRenamedConsumerDefinition : ConsumerDefinition<BoardRenamedConsumer>
{
    public BoardRenamedConsumerDefinition()
    {
        EndpointName = "notrelix-work-board-renamed-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardRenamedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class BoardArchivedConsumerDefinition : ConsumerDefinition<BoardArchivedConsumer>
{
    public BoardArchivedConsumerDefinition()
    {
        EndpointName = "notrelix-work-board-archived-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardArchivedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class BoardUnarchivedConsumerDefinition : ConsumerDefinition<BoardUnarchivedConsumer>
{
    public BoardUnarchivedConsumerDefinition()
    {
        EndpointName = "notrelix-work-board-unarchived-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardUnarchivedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class BoardItemRenamedConsumerDefinition : ConsumerDefinition<BoardItemRenamedConsumer>
{
    public BoardItemRenamedConsumerDefinition()
    {
        EndpointName = "notrelix-work-board-item-renamed-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemRenamedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class BoardItemMovedConsumerDefinition : ConsumerDefinition<BoardItemMovedConsumer>
{
    public BoardItemMovedConsumerDefinition()
    {
        EndpointName = "notrelix-work-board-item-moved-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemMovedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class BoardItemArchivedConsumerDefinition : ConsumerDefinition<BoardItemArchivedConsumer>
{
    public BoardItemArchivedConsumerDefinition()
    {
        EndpointName = "notrelix-work-board-item-archived-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardItemArchivedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class BoardFieldCreatedConsumerDefinition : ConsumerDefinition<BoardFieldCreatedConsumer>
{
    public BoardFieldCreatedConsumerDefinition()
    {
        EndpointName = "notrelix-work-board-field-created-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardFieldCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class BoardFieldUpdatedConsumerDefinition : ConsumerDefinition<BoardFieldUpdatedConsumer>
{
    public BoardFieldUpdatedConsumerDefinition()
    {
        EndpointName = "notrelix-work-board-field-updated-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardFieldUpdatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class BoardFieldDeletedConsumerDefinition : ConsumerDefinition<BoardFieldDeletedConsumer>
{
    public BoardFieldDeletedConsumerDefinition()
    {
        EndpointName = "notrelix-work-board-field-deleted-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardFieldDeletedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class BoardViewCreatedConsumerDefinition : ConsumerDefinition<BoardViewCreatedConsumer>
{
    public BoardViewCreatedConsumerDefinition()
    {
        EndpointName = "notrelix-work-board-view-created-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardViewCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class BoardViewDeletedConsumerDefinition : ConsumerDefinition<BoardViewDeletedConsumer>
{
    public BoardViewDeletedConsumerDefinition()
    {
        EndpointName = "notrelix-work-board-view-deleted-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<BoardViewDeletedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class LabelCreatedConsumerDefinition : ConsumerDefinition<LabelCreatedConsumer>
{
    public LabelCreatedConsumerDefinition()
    {
        EndpointName = "notrelix-work-label-created-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<LabelCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class LabelUpdatedConsumerDefinition : ConsumerDefinition<LabelUpdatedConsumer>
{
    public LabelUpdatedConsumerDefinition()
    {
        EndpointName = "notrelix-work-label-updated-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<LabelUpdatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class ChecklistCreatedConsumerDefinition : ConsumerDefinition<ChecklistCreatedConsumer>
{
    public ChecklistCreatedConsumerDefinition()
    {
        EndpointName = "notrelix-work-checklist-created-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChecklistCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}

public sealed class ChecklistItemToggledConsumerDefinition : ConsumerDefinition<ChecklistItemToggledConsumer>
{
    public ChecklistItemToggledConsumerDefinition()
    {
        EndpointName = "notrelix-work-checklist-item-toggled-v1";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChecklistItemToggledConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 2;
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Interval(3, TimeSpan.FromMilliseconds(200));
        });
    }
}
