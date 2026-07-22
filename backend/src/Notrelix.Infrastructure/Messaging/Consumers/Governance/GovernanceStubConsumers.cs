using Notrelix.Application.Events.Governance;

namespace Notrelix.Infrastructure.Messaging.Consumers.Governance;

public sealed class CustomRoleAssignedConsumer : IConsumer<CustomRoleAssignedIntegrationEvent>
{
    private readonly ILogger<CustomRoleAssignedConsumer> _logger;

    public CustomRoleAssignedConsumer(ILogger<CustomRoleAssignedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<CustomRoleAssignedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Governance] CustomRoleAssigned: RoleId={RoleId}, RoleName={RoleName}, UserId={UserId}, WorkspaceId={WorkspaceId}",
            context.Message.RoleId,
            context.Message.RoleName,
            context.Message.UserId,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class ResourcePermissionGrantedConsumer : IConsumer<ResourcePermissionGrantedIntegrationEvent>
{
    private readonly ILogger<ResourcePermissionGrantedConsumer> _logger;

    public ResourcePermissionGrantedConsumer(ILogger<ResourcePermissionGrantedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ResourcePermissionGrantedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Governance] ResourcePermissionGranted: PermissionId={PermissionId}, ResourceType={ResourceType}, ResourceId={ResourceId}, SubjectType={SubjectType}, SubjectId={SubjectId}, PermissionLevel={PermissionLevel}, WorkspaceId={WorkspaceId}",
            context.Message.PermissionId,
            context.Message.ResourceType,
            context.Message.ResourceId,
            context.Message.SubjectType,
            context.Message.SubjectId,
            context.Message.PermissionLevel,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

public sealed class ResourcePermissionRevokedConsumer : IConsumer<ResourcePermissionRevokedIntegrationEvent>
{
    private readonly ILogger<ResourcePermissionRevokedConsumer> _logger;

    public ResourcePermissionRevokedConsumer(ILogger<ResourcePermissionRevokedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ResourcePermissionRevokedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "[Governance] ResourcePermissionRevoked: PermissionId={PermissionId}, ResourceType={ResourceType}, ResourceId={ResourceId}, SubjectType={SubjectType}, SubjectId={SubjectId}, WorkspaceId={WorkspaceId}",
            context.Message.PermissionId,
            context.Message.ResourceType,
            context.Message.ResourceId,
            context.Message.SubjectType,
            context.Message.SubjectId,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}
