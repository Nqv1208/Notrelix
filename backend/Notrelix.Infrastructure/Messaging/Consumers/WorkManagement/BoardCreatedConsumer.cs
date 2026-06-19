using MassTransit;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Infrastructure.Messaging.Consumers.WorkManagement;

public sealed class BoardCreatedConsumer : IConsumer<BoardCreatedIntegrationEvent>
{
    private readonly ILogger<BoardCreatedConsumer> _logger;

    public BoardCreatedConsumer(ILogger<BoardCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BoardCreatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[WorkManagement] BoardCreated: BoardId={BoardId}, WorkspaceId={WorkspaceId}, Name={Name}",
            context.Message.BoardId,
            context.Message.WorkspaceId,
            context.Message.Name);
        return Task.CompletedTask;
    }
}
