using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Infrastructure.Messaging.Consumers.WorkManagement;

public sealed class BoardRenamedConsumer : IConsumer<BoardRenamedIntegrationEvent>
{
    private readonly ILogger<BoardRenamedConsumer> _logger;

    public BoardRenamedConsumer(ILogger<BoardRenamedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardRenamedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardRenamed: BoardId={BoardId}, OldName={OldName}, NewName={NewName}, WorkspaceId={WorkspaceId}",
            context.Message.BoardId,
            context.Message.OldName,
            context.Message.NewName,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class BoardArchivedConsumer : IConsumer<BoardArchivedIntegrationEvent>
{
    private readonly ILogger<BoardArchivedConsumer> _logger;

    public BoardArchivedConsumer(ILogger<BoardArchivedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardArchivedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardArchived: BoardId={BoardId}, WorkspaceId={WorkspaceId}",
            context.Message.BoardId,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class BoardUnarchivedConsumer : IConsumer<BoardUnarchivedIntegrationEvent>
{
    private readonly ILogger<BoardUnarchivedConsumer> _logger;

    public BoardUnarchivedConsumer(ILogger<BoardUnarchivedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardUnarchivedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardUnarchived: BoardId={BoardId}, WorkspaceId={WorkspaceId}",
            context.Message.BoardId,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class BoardItemRenamedConsumer : IConsumer<BoardItemRenamedIntegrationEvent>
{
    private readonly ILogger<BoardItemRenamedConsumer> _logger;

    public BoardItemRenamedConsumer(ILogger<BoardItemRenamedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardItemRenamedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardItemRenamed: ItemId={ItemId}, BoardId={BoardId}, OldName={OldName}, NewName={NewName}, WorkspaceId={WorkspaceId}",
            context.Message.ItemId,
            context.Message.BoardId,
            context.Message.OldName,
            context.Message.NewName,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class BoardItemMovedConsumer : IConsumer<BoardItemMovedIntegrationEvent>
{
    private readonly ILogger<BoardItemMovedConsumer> _logger;

    public BoardItemMovedConsumer(ILogger<BoardItemMovedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardItemMovedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardItemMoved: ItemId={ItemId}, BoardId={BoardId}, OldGroupId={OldGroupId}, NewGroupId={NewGroupId}, WorkspaceId={WorkspaceId}",
            context.Message.ItemId,
            context.Message.BoardId,
            context.Message.OldGroupId,
            context.Message.NewGroupId,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class BoardItemArchivedConsumer : IConsumer<BoardItemArchivedIntegrationEvent>
{
    private readonly ILogger<BoardItemArchivedConsumer> _logger;

    public BoardItemArchivedConsumer(ILogger<BoardItemArchivedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardItemArchivedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardItemArchived: ItemId={ItemId}, BoardId={BoardId}, WorkspaceId={WorkspaceId}",
            context.Message.ItemId,
            context.Message.BoardId,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class BoardFieldCreatedConsumer : IConsumer<BoardFieldCreatedIntegrationEvent>
{
    private readonly ILogger<BoardFieldCreatedConsumer> _logger;

    public BoardFieldCreatedConsumer(ILogger<BoardFieldCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardFieldCreatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardFieldCreated: FieldId={FieldId}, BoardId={BoardId}, FieldName={FieldName}, FieldType={FieldType}, WorkspaceId={WorkspaceId}",
            context.Message.FieldId,
            context.Message.BoardId,
            context.Message.FieldName,
            context.Message.FieldType,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class BoardFieldUpdatedConsumer : IConsumer<BoardFieldUpdatedIntegrationEvent>
{
    private readonly ILogger<BoardFieldUpdatedConsumer> _logger;

    public BoardFieldUpdatedConsumer(ILogger<BoardFieldUpdatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardFieldUpdatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardFieldUpdated: FieldId={FieldId}, BoardId={BoardId}, WorkspaceId={WorkspaceId}",
            context.Message.FieldId,
            context.Message.BoardId,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class BoardFieldDeletedConsumer : IConsumer<BoardFieldDeletedIntegrationEvent>
{
    private readonly ILogger<BoardFieldDeletedConsumer> _logger;

    public BoardFieldDeletedConsumer(ILogger<BoardFieldDeletedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardFieldDeletedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardFieldDeleted: FieldId={FieldId}, BoardId={BoardId}, WorkspaceId={WorkspaceId}",
            context.Message.FieldId,
            context.Message.BoardId,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class BoardViewCreatedConsumer : IConsumer<BoardViewCreatedIntegrationEvent>
{
    private readonly ILogger<BoardViewCreatedConsumer> _logger;

    public BoardViewCreatedConsumer(ILogger<BoardViewCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardViewCreatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardViewCreated: ViewId={ViewId}, BoardId={BoardId}, ViewName={ViewName}, WorkspaceId={WorkspaceId}",
            context.Message.ViewId,
            context.Message.BoardId,
            context.Message.ViewName,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class BoardViewDeletedConsumer : IConsumer<BoardViewDeletedIntegrationEvent>
{
    private readonly ILogger<BoardViewDeletedConsumer> _logger;

    public BoardViewDeletedConsumer(ILogger<BoardViewDeletedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardViewDeletedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardViewDeleted: ViewId={ViewId}, BoardId={BoardId}, WorkspaceId={WorkspaceId}",
            context.Message.ViewId,
            context.Message.BoardId,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class LabelCreatedConsumer : IConsumer<LabelCreatedIntegrationEvent>
{
    private readonly ILogger<LabelCreatedConsumer> _logger;

    public LabelCreatedConsumer(ILogger<LabelCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<LabelCreatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] LabelCreated: LabelId={LabelId}, BoardId={BoardId}, LabelName={LabelName}, WorkspaceId={WorkspaceId}",
            context.Message.LabelId,
            context.Message.BoardId,
            context.Message.LabelName,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class LabelUpdatedConsumer : IConsumer<LabelUpdatedIntegrationEvent>
{
    private readonly ILogger<LabelUpdatedConsumer> _logger;

    public LabelUpdatedConsumer(ILogger<LabelUpdatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<LabelUpdatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] LabelUpdated: LabelId={LabelId}, BoardId={BoardId}, WorkspaceId={WorkspaceId}",
            context.Message.LabelId,
            context.Message.BoardId,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class ChecklistCreatedConsumer : IConsumer<ChecklistCreatedIntegrationEvent>
{
    private readonly ILogger<ChecklistCreatedConsumer> _logger;

    public ChecklistCreatedConsumer(ILogger<ChecklistCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ChecklistCreatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] ChecklistCreated: ChecklistId={ChecklistId}, ItemId={ItemId}, BoardId={BoardId}, ChecklistTitle={ChecklistTitle}, WorkspaceId={WorkspaceId}",
            context.Message.ChecklistId,
            context.Message.ItemId,
            context.Message.BoardId,
            context.Message.ChecklistTitle,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class ChecklistItemToggledConsumer : IConsumer<ChecklistItemToggledIntegrationEvent>
{
    private readonly ILogger<ChecklistItemToggledConsumer> _logger;

    public ChecklistItemToggledConsumer(ILogger<ChecklistItemToggledConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ChecklistItemToggledIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] ChecklistItemToggled: ChecklistId={ChecklistId}, ChecklistItemId={ChecklistItemId}, ItemId={ItemId}, BoardId={BoardId}, IsCompleted={IsCompleted}, WorkspaceId={WorkspaceId}",
            context.Message.ChecklistId,
            context.Message.ChecklistItemId,
            context.Message.ItemId,
            context.Message.BoardId,
            context.Message.IsCompleted,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}
