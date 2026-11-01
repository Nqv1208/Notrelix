using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Governance.Authorization;

namespace Notrelix.Application.Features.Governance.Roles.Queries.GetPermissionCatalog;

/// <summary>The product-defined permission catalog (UC-GOV-C06). No tenant data: any signed-in person may read it.</summary>
public sealed record GetPermissionCatalogQuery : IQuery<Result<PermissionCatalogDto>>, IAuthenticatedRequest, IGlobalRequest, INoDataRequest;

public sealed class GetPermissionCatalogQueryHandler : IRequestHandler<GetPermissionCatalogQuery, Result<PermissionCatalogDto>>
{
    public Task<Result<PermissionCatalogDto>> Handle(GetPermissionCatalogQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result<PermissionCatalogDto>.Success(new PermissionCatalogDto(PermissionCatalog.Entries)));
}
