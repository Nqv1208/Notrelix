using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.Common;
using global::Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetPage;

public record GetPageQuery(Guid PageId) : IQuery<Result<PageDto>>, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class GetPageQueryHandler : IRequestHandler<GetPageQuery, Result<PageDto>>
{
    private readonly IDocumentDbContext _context;
    public GetPageQueryHandler(IDocumentDbContext context) => _context = context;

    public async Task<Result<PageDto>> Handle(GetPageQuery request, CancellationToken ct)
    {
        var page = await _context.Pages.AsNoTracking()
            .FirstOrDefaultAsync(page => page.Id == request.PageId && !page.IsDeleted, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        return Result<PageDto>.Success(DocumentDtoMapper.ToPageDto(page));
    }
}
