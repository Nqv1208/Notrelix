using Notrelix.Application.Common.Models;
using Notrelix.Application.Events.Integrations;
using Notrelix.Application.Features.Integrations.Abstractions;

namespace Notrelix.Application.Features.Integrations.Connections.Commands.TestIntegration;

/// <summary>
/// Sends a test delivery to an outgoing webhook through the outbox, the way every delivery goes: signed,
/// retried under one delivery identity, and its outcome recorded on the connection (last success or last
/// error). It works while the connection is paused. OAuth providers have no test delivery.
/// </summary>
public record TestIntegrationCommand(Guid WorkspaceId, Guid ConnectionId)
    : ICommand<Result<IntegrationTestDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageIntegrations;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class TestIntegrationCommandHandler : IRequestHandler<TestIntegrationCommand, Result<IntegrationTestDto>>
{
    private readonly IIntegrationDbContext _context;
    private readonly IIntegrationEventCollector _events;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public TestIntegrationCommandHandler(
        IIntegrationDbContext context,
        IIntegrationEventCollector events,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock)
    {
        _context = context;
        _events = events;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<IntegrationTestDto>> Handle(TestIntegrationCommand request, CancellationToken cancellationToken)
    {
        var connection = await IntegrationConnectionLookup.RequireAsync(_context, request.WorkspaceId, request.ConnectionId, cancellationToken);
        if (connection.Provider != IntegrationProvider.Webhook)
        {
            return Result<IntegrationTestDto>.Failure(new ApplicationError(
                "integrations.test.unsupported", "Only an outgoing webhook has a test delivery.", ApplicationErrorType.BusinessRule));
        }

        var deliveryId = Guid.CreateVersion7();
        _events.Add(new IntegrationWebhookDispatchRequestedV1(
            EventId: Guid.CreateVersion7(),
            DeliveryId: deliveryId,
            ConnectionId: connection.Id,
            DeliveryType: IntegrationWebhookDispatchRequestedV1.TestType,
            AccountIdValue: connection.AccountId,
            WorkspaceIdValue: connection.WorkspaceId,
            OccurredAt: _clock.UtcNow,
            CorrelationId: Guid.CreateVersion7(),
            ActorUserIdValue: _requestContext.UserId));

        return Result<IntegrationTestDto>.Success(new IntegrationTestDto(deliveryId));
    }
}
