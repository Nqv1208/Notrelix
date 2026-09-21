using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Infrastructure.Messaging.Consumers.WorkManagement;

public sealed class BoardItemCreatedConsumer : IConsumer<BoardItemCreatedIntegrationEventV2>
{
    private readonly ILogger<BoardItemCreatedConsumer> _logger;

    public BoardItemCreatedConsumer(ILogger<BoardItemCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardItemCreatedIntegrationEventV2> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardItemCreated: ItemId={ItemId}, BoardId={BoardId}, Title={Title}, Revision={Revision}",
            context.Message.ItemId,
            context.Message.BoardId,
            context.Message.Title,
            context.Message.Revision);
        return Task.CompletedTask;
    }
}
