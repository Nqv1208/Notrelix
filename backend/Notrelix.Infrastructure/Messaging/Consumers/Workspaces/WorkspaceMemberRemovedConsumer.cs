using MassTransit;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Events.Workspaces;

namespace Notrelix.Infrastructure.Messaging.Consumers.Workspaces;

public sealed class WorkspaceMemberRemovedConsumer : IConsumer<WorkspaceMemberRemovedIntegrationEvent>
{
    private readonly ILogger<WorkspaceMemberRemovedConsumer> _logger;

    public WorkspaceMemberRemovedConsumer(ILogger<WorkspaceMemberRemovedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<WorkspaceMemberRemovedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Workspaces] WorkspaceMemberRemoved: WorkspaceId={WorkspaceId}, UserId={UserId}",
            context.Message.WorkspaceId,
            context.Message.UserId);
        return Task.CompletedTask;
    }
}
