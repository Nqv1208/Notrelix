using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Abstractions;

namespace Notrelix.Application.Features.Integrations.Connections.Commands.PauseIntegration;

public record PauseIntegrationCommand(Guid WorkspaceId, Guid ConnectionId)
    : ICommand<Result<IntegrationConnectionDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageIntegrations;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class PauseIntegrationCommandHandler : IRequestHandler<PauseIntegrationCommand, Result<IntegrationConnectionDto>>
{
    private readonly IIntegrationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public PauseIntegrationCommandHandler(IIntegrationDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider clock)
    {
        _context = context;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<IntegrationConnectionDto>> Handle(PauseIntegrationCommand request, CancellationToken cancellationToken)
    {
        var connection = await IntegrationConnectionLookup.RequireAsync(_context, request.WorkspaceId, request.ConnectionId, cancellationToken);
        connection.Pause(_requestContext.UserId, _clock.UtcNow);
        return Result<IntegrationConnectionDto>.Success(IntegrationConnectionMapper.ToDto(connection));
    }
}
