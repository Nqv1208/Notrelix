using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Infrastructure.Messaging.Consumers.Collaboration;

/// <summary>Endpoint settings shared by the Item history consumers; each concrete definition names its own endpoint.</summary>
public abstract class ItemActivityConsumerDefinition<TConsumer> : ConsumerDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
    protected ItemActivityConsumerDefinition(string endpointName)
    {
        EndpointName = endpointName;
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<TConsumer> consumerConfigurator,
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

public sealed class ItemCreatedActivityConsumerDefinition : ItemActivityConsumerDefinition<ItemCreatedActivityConsumer>
{
    public ItemCreatedActivityConsumerDefinition() : base("notrelix-activity-item-created-v1") { }
}

public sealed class ItemRenamedActivityConsumerDefinition : ItemActivityConsumerDefinition<ItemRenamedActivityConsumer>
{
    public ItemRenamedActivityConsumerDefinition() : base("notrelix-activity-item-renamed-v1") { }
}

public sealed class ItemFieldChangedActivityConsumerDefinition : ItemActivityConsumerDefinition<ItemFieldChangedActivityConsumer>
{
    public ItemFieldChangedActivityConsumerDefinition() : base("notrelix-activity-item-field-changed-v1") { }
}

public sealed class ItemMovedActivityConsumerDefinition : ItemActivityConsumerDefinition<ItemMovedActivityConsumer>
{
    public ItemMovedActivityConsumerDefinition() : base("notrelix-activity-item-moved-v1") { }
}

public sealed class ItemMovedToBoardActivityConsumerDefinition : ItemActivityConsumerDefinition<ItemMovedToBoardActivityConsumer>
{
    public ItemMovedToBoardActivityConsumerDefinition() : base("notrelix-activity-item-moved-to-board-v1") { }
}

public sealed class ItemAssignedActivityConsumerDefinition : ItemActivityConsumerDefinition<ItemAssignedActivityConsumer>
{
    public ItemAssignedActivityConsumerDefinition() : base("notrelix-activity-item-assigned-v1") { }
}

public sealed class ItemUnassignedActivityConsumerDefinition : ItemActivityConsumerDefinition<ItemUnassignedActivityConsumer>
{
    public ItemUnassignedActivityConsumerDefinition() : base("notrelix-activity-item-unassigned-v1") { }
}

public sealed class ItemLabelAddedActivityConsumerDefinition : ItemActivityConsumerDefinition<ItemLabelAddedActivityConsumer>
{
    public ItemLabelAddedActivityConsumerDefinition() : base("notrelix-activity-item-label-added-v1") { }
}

public sealed class ItemLabelRemovedActivityConsumerDefinition : ItemActivityConsumerDefinition<ItemLabelRemovedActivityConsumer>
{
    public ItemLabelRemovedActivityConsumerDefinition() : base("notrelix-activity-item-label-removed-v1") { }
}

public sealed class ItemArchivedActivityConsumerDefinition : ItemActivityConsumerDefinition<ItemArchivedActivityConsumer>
{
    public ItemArchivedActivityConsumerDefinition() : base("notrelix-activity-item-archived-v1") { }
}

public sealed class ItemUnarchivedActivityConsumerDefinition : ItemActivityConsumerDefinition<ItemUnarchivedActivityConsumer>
{
    public ItemUnarchivedActivityConsumerDefinition() : base("notrelix-activity-item-unarchived-v1") { }
}

public sealed class ItemDeletedActivityConsumerDefinition : ItemActivityConsumerDefinition<ItemDeletedActivityConsumer>
{
    public ItemDeletedActivityConsumerDefinition() : base("notrelix-activity-item-deleted-v1") { }
}

public sealed class ItemRestoredActivityConsumerDefinition : ItemActivityConsumerDefinition<ItemRestoredActivityConsumer>
{
    public ItemRestoredActivityConsumerDefinition() : base("notrelix-activity-item-restored-v1") { }
}
