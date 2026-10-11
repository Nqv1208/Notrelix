using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.Common;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Governance.Public.Visibility;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetPage;

public record GetPageQuery(Guid PageId) : IQuery<Result<PageDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewPage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class GetPageQueryHandler : IRequestHandler<GetPageQuery, Result<PageDto>>
{
    private readonly IDocumentDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetPageQueryHandler(IDocumentDbContext context, IResourceVisibility visibility, ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<PageDto>> Handle(GetPageQuery request, CancellationToken ct)
    {
        var page = await _context.Pages.AsNoTracking()
            .FirstOrDefaultAsync(page => page.Id == request.PageId && page.DeletedAt == null, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        IReadOnlyCollection<Guid> spaces = page.SpaceId is null
            ? []
            : await _visibility.VisibleSpaceIdsAsync(page.WorkspaceId, _requestContext.UserId, ct);
        return Result<PageDto>.Success(DocumentDtoMapper.ToPageDto(page, spaces));
    }
}
