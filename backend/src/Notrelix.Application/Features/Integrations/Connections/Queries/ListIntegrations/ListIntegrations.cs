using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Abstractions;

namespace Notrelix.Application.Features.Integrations.Connections.Queries.ListIntegrations;

/// <summary>
/// The catalog (with whether each provider has a client id and secret in configuration) and the
/// workspace's connections. Members who manage integrations see them; revoked connections are gone.
/// </summary>
public record ListIntegrationsQuery(Guid WorkspaceId)
    : IQuery<Result<IntegrationsOverviewDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageIntegrations;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class ListIntegrationsQueryHandler : IRequestHandler<ListIntegrationsQuery, Result<IntegrationsOverviewDto>>
{
    private readonly IIntegrationDbContext _context;
    private readonly IIntegrationProviderSettings _settings;

    public ListIntegrationsQueryHandler(IIntegrationDbContext context, IIntegrationProviderSettings settings)
    {
        _context = context;
        _settings = settings;
    }

    public async Task<Result<IntegrationsOverviewDto>> Handle(ListIntegrationsQuery request, CancellationToken cancellationToken)
    {
        var connections = await _context.IntegrationConnections
            .AsNoTracking()
            .Where(c => c.WorkspaceId == request.WorkspaceId
                && c.DeletedAt == null
                && c.Status != IntegrationConnectionStatus.Revoked)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var catalog = IntegrationCatalog.All
            .Select(entry => new IntegrationCatalogEntryDto(
                entry.Key,
                entry.Method == IntegrationConnectMethod.Webhook || _settings.IsConfigured(entry.Provider)))
            .ToList();

        return Result<IntegrationsOverviewDto>.Success(new IntegrationsOverviewDto(
            catalog,
            connections.Select(IntegrationConnectionMapper.ToDto).ToList()));
    }
}
