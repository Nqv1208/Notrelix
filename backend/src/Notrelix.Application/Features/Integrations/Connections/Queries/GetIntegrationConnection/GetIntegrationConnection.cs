using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Abstractions;

namespace Notrelix.Application.Features.Integrations.Connections.Queries.GetIntegrationConnection;

/// <summary>
/// One connection in detail: its status, the last success and the last error, the address (for an outgoing
/// webhook) and the scopes granted to it. The DTO carries no signing secret, token, digest or secret reference.
/// Read by the members who manage integrations, under the same permission as the list. A connection of another
/// workspace, a revoked or deleted one, and an id that does not exist are all NotFound.
/// </summary>
public sealed record GetIntegrationConnectionQuery(Guid WorkspaceId, Guid ConnectionId)
    : IQuery<Result<IntegrationConnectionDetailDto>>, IReadRequest, IWorkspaceRequest, IAuthenticatedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageIntegrations;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public sealed class GetIntegrationConnectionQueryHandler
    : IRequestHandler<GetIntegrationConnectionQuery, Result<IntegrationConnectionDetailDto>>
{
    private readonly IIntegrationDbContext _context;

    public GetIntegrationConnectionQueryHandler(IIntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IntegrationConnectionDetailDto>> Handle(
        GetIntegrationConnectionQuery request, CancellationToken cancellationToken)
    {
        var connection = await IntegrationConnectionLookup.RequireAsync(
            _context, request.WorkspaceId, request.ConnectionId, cancellationToken);

        var scopes = await _context.IntegrationScopes
            .AsNoTracking()
            .Where(s => s.ConnectionId == connection.Id)
            .OrderBy(s => s.Scope)
            .Select(s => s.Scope)
            .ToListAsync(cancellationToken);

        return Result<IntegrationConnectionDetailDto>.Success(new IntegrationConnectionDetailDto(
            IntegrationConnectionMapper.ToDto(connection),
            scopes));
    }
}
