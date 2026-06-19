using MassTransit;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Events.Workspaces;

namespace Notrelix.Infrastructure.Messaging.Consumers.Workspaces;

public sealed class WorkspaceMemberAddedConsumer : IConsumer<WorkspaceMemberAddedIntegrationEvent>
{
    private readonly ILogger<WorkspaceMemberAddedConsumer> _logger;

    public WorkspaceMemberAddedConsumer(ILogger<WorkspaceMemberAddedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<WorkspaceMemberAddedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Workspaces] WorkspaceMemberAdded: WorkspaceId={WorkspaceId}, UserId={UserId}, Role={Role}",
            context.Message.WorkspaceId,
            context.Message.UserId,
            context.Message.Role);
        return Task.CompletedTask;
    }
}
