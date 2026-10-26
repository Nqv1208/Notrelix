using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Abstractions;

namespace Notrelix.Application.Features.Integrations.Connections.Commands.DisconnectIntegration;

/// <summary>
/// Disconnects a connection of the workspace: it is revoked (its secrets are revoked after commit by the
/// revoked-connection consumer) and any calendar binding that used it stops syncing.
/// </summary>
public record DisconnectIntegrationCommand(Guid WorkspaceId, Guid ConnectionId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageIntegrations;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class DisconnectIntegrationCommandHandler : IRequestHandler<DisconnectIntegrationCommand, Result>
{
    private readonly IIntegrationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public DisconnectIntegrationCommandHandler(IIntegrationDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider clock)
    {
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result> Handle(DisconnectIntegrationCommand request, CancellationToken cancellationToken)
    {
        var connection = await IntegrationConnectionLookup.RequireAsync(_context, request.WorkspaceId, request.ConnectionId, cancellationToken);
        var actorId = _requestContext.UserId;
        var now = _clock.UtcNow;

        var bindings = await _context.CalendarIntegrations
            .Where(ci => ci.ConnectionId == connection.Id && ci.IsActive && ci.DeletedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var binding in bindings) binding.Deactivate(actorId, now);

        connection.Disconnect(actorId, now);
        return Result.Success();
    }
}
