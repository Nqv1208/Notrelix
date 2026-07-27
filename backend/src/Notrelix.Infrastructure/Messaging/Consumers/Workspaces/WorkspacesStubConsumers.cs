using Notrelix.Application.Events.Workspaces;

namespace Notrelix.Infrastructure.Messaging.Consumers.Workspaces;

public sealed class WorkspaceArchivedConsumer : IConsumer<WorkspaceArchivedIntegrationEvent>
{
    private readonly ILogger<WorkspaceArchivedConsumer> _logger;

    public WorkspaceArchivedConsumer(ILogger<WorkspaceArchivedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<WorkspaceArchivedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Workspaces] WorkspaceArchived: WorkspaceId={WorkspaceId}",
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class WorkspaceUnarchivedConsumer : IConsumer<WorkspaceUnarchivedIntegrationEvent>
{
    private readonly ILogger<WorkspaceUnarchivedConsumer> _logger;

    public WorkspaceUnarchivedConsumer(ILogger<WorkspaceUnarchivedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<WorkspaceUnarchivedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Workspaces] WorkspaceUnarchived: WorkspaceId={WorkspaceId}",
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class TeamCreatedConsumer : IConsumer<TeamCreatedIntegrationEvent>
{
    private readonly ILogger<TeamCreatedConsumer> _logger;

    public TeamCreatedConsumer(ILogger<TeamCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TeamCreatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Workspaces] TeamCreated: TeamId={TeamId}, Name={Name}, WorkspaceId={WorkspaceId}",
            context.Message.TeamId,
            context.Message.Name,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class SpaceCreatedConsumer : IConsumer<SpaceCreatedIntegrationEvent>
{
    private readonly ILogger<SpaceCreatedConsumer> _logger;

    public SpaceCreatedConsumer(ILogger<SpaceCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<SpaceCreatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Workspaces] SpaceCreated: SpaceId={SpaceId}, Name={Name}, Visibility={Visibility}, WorkspaceId={WorkspaceId}",
            context.Message.SpaceId,
            context.Message.Name,
            context.Message.Visibility,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}
