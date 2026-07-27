using Notrelix.Application.Events.Documents;

namespace Notrelix.Infrastructure.Messaging.Consumers.Documents;

public sealed class PageCreatedConsumer : IConsumer<PageCreatedIntegrationEvent>
{
    private readonly ILogger<PageCreatedConsumer> _logger;

    public PageCreatedConsumer(ILogger<PageCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<PageCreatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Documents] PageCreated: PageId={PageId}, Title={Title}, WorkspaceId={WorkspaceId}",
            context.Message.PageId,
            context.Message.Title,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class PageArchivedConsumer : IConsumer<PageArchivedIntegrationEvent>
{
    private readonly ILogger<PageArchivedConsumer> _logger;

    public PageArchivedConsumer(ILogger<PageArchivedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<PageArchivedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Documents] PageArchived: PageId={PageId}, WorkspaceId={WorkspaceId}",
            context.Message.PageId,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}
