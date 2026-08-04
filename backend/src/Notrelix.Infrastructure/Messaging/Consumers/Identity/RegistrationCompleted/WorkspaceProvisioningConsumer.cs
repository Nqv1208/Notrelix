using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Features.Workspaces.Provisioning.Commands.ProvisionPersonalWorkspace;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.RegistrationCompleted;

public sealed class WorkspaceProvisioningConsumer : IConsumer<IdentityRegistrationCompletedIntegrationEventV1>
{
    private readonly ISender _sender;
    private readonly IIdempotencyExecutionContextWriter _executionContextWriter;
    private readonly ILogger<WorkspaceProvisioningConsumer> _logger;

    public WorkspaceProvisioningConsumer(
        ISender sender,
        IIdempotencyExecutionContextWriter executionContextWriter,
        ILogger<WorkspaceProvisioningConsumer> logger)
    {
        _sender = sender;
        _executionContextWriter = executionContextWriter;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IdentityRegistrationCompletedIntegrationEventV1> context)
    {
        var msg = context.Message;

        // Spec 3.4: message-source execution keys are the event/message id in N format.
        _executionContextWriter.Set(msg.EventId.ToString("N"), IdempotencyExecutionSource.Message);

        var result = await _sender.Send(new ProvisionPersonalWorkspaceCommand(
            UserId: msg.UserId,
            AccountId: msg.AccountIdValue,
            WorkspaceName: msg.DisplayName,
            MessageId: msg.EventId,
            SourceEventId: msg.SourceEventId,
            SourceMessageName: msg.MessageName,
            SourceMessageVersion: msg.SchemaVersion,
            CorrelationId: msg.CorrelationId.ToString(),
            CausationId: msg.CausationId?.ToString(),
            OccurredAt: msg.OccurredAt
        ), context.CancellationToken);

        _logger.LogInformation(
            "Workspace provisioning for {UserId} in account {AccountId}: {Status}",
            msg.UserId,
            msg.AccountId,
            result.AlreadyExisted ? "already-existed" : "created");
    }
}