using MassTransit;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Events.Identity;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity;

public sealed class WorkspaceProvisioningConsumer : IConsumer<UserRegisteredIntegrationEvent>
{
    private readonly WorkspaceProvisioningService _service;
    private readonly ILogger<WorkspaceProvisioningConsumer> _logger;

    public WorkspaceProvisioningConsumer(WorkspaceProvisioningService service, ILogger<WorkspaceProvisioningConsumer> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegisteredIntegrationEvent> context)
    {
        var message = context.Message;
        await _service.ProvisionPersonalWorkspace(message.UserId, message.Email, message.EventId, message.OccurredAt, context.CancellationToken);
        _logger.LogInformation("Processed workspace provisioning for user {UserId}", message.UserId);
    }
}
