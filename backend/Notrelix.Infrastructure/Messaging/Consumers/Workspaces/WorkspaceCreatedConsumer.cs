using MassTransit;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Events.Workspaces;

namespace Notrelix.Infrastructure.Messaging.Consumers.Workspaces;

public sealed class WorkspaceCreatedConsumer : IConsumer<WorkspaceCreatedIntegrationEvent>
{
    private readonly ILogger<WorkspaceCreatedConsumer> _logger;

    public WorkspaceCreatedConsumer(ILogger<WorkspaceCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<WorkspaceCreatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Workspaces] WorkspaceCreated: WorkspaceId={WorkspaceId}, Name={Name}, Slug={Slug}",
            context.Message.WorkspaceId,
            context.Message.Name,
            context.Message.Slug);
        return Task.CompletedTask;
    }
}
