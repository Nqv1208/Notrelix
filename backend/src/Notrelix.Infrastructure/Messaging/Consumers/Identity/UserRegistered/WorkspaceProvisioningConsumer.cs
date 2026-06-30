using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Workspaces.Provisioning.Commands.ProvisionPersonalWorkspace;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.UserRegistered;

public sealed class WorkspaceProvisioningConsumer : IConsumer<UserRegisteredIntegrationEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<WorkspaceProvisioningConsumer> _logger;

    public WorkspaceProvisioningConsumer(ISender sender, ILogger<WorkspaceProvisioningConsumer> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegisteredIntegrationEvent> context)
    {
        var msg = context.Message;

        var result = await _sender.Send(new ProvisionPersonalWorkspaceCommand(
            UserId: msg.UserId,
            Email: msg.Email,
            MessageId: msg.EventId,
            SourceEventId: msg.SourceEventId,
            SourceMessageName: msg.MessageName,
            SourceMessageVersion: msg.SchemaVersion,
            CorrelationId: msg.CorrelationId,
            CausationId: msg.CausationId,
            OccurredAt: msg.OccurredAt
        ), context.CancellationToken);

        _logger.LogInformation(
            "Workspace provisioning for {UserId}: {Status}",
            msg.UserId,
            result.AlreadyExisted ? "already-existed" : "created");
    }
}
