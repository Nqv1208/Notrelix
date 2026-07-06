using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Infrastructure.Messaging.Consumers.WorkManagement;

public sealed class BoardItemCreatedConsumer : IConsumer<BoardItemCreatedIntegrationEvent>
{
    private readonly ILogger<BoardItemCreatedConsumer> _logger;

    public BoardItemCreatedConsumer(ILogger<BoardItemCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardItemCreatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardItemCreated: ItemId={ItemId}, BoardId={BoardId}, Title={Title}",
            context.Message.ItemId,
            context.Message.BoardId,
            context.Message.Title);
        return Task.CompletedTask;
    }
}
