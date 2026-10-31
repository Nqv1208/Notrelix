using Notrelix.Application.Events.Governance;

namespace Notrelix.Infrastructure.Messaging.Consumers.Governance;

[OrderingNotApplicable(OrderingReasons.LogOnly)]
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
            "[Governance] ResourcePermissionGranted: PermissionId={PermissionId}, ResourceKind={ResourceKind}, ResourceId={ResourceId}, SubjectType={SubjectType}, SubjectId={SubjectId}, PermissionLevel={PermissionLevel}, WorkspaceId={WorkspaceId}",
            context.Message.PermissionId,
            context.Message.ResourceKind,
            context.Message.ResourceId,
            context.Message.SubjectType,
            context.Message.SubjectId,
            context.Message.PermissionLevel,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}

[OrderingNotApplicable(OrderingReasons.LogOnly)]
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
            "[Governance] ResourcePermissionRevoked: PermissionId={PermissionId}, ResourceKind={ResourceKind}, ResourceId={ResourceId}, SubjectType={SubjectType}, SubjectId={SubjectId}, WorkspaceId={WorkspaceId}",
            context.Message.PermissionId,
            context.Message.ResourceKind,
            context.Message.ResourceId,
            context.Message.SubjectType,
            context.Message.SubjectId,
            context.Message.WorkspaceId);
        return Task.CompletedTask;
    }
}
