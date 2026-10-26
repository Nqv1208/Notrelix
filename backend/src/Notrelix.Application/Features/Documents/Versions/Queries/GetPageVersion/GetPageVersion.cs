using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Domain.Documents.Versions;

namespace Notrelix.Application.Features.Documents.Versions.Queries.GetPageVersion;

/// <summary>The captured content of one entry, read-only (a snapshot is not a second live document, DCT-017).</summary>
public record GetPageVersionQuery(Guid PageId, Guid VersionId) : IQuery<Result<PageVersionDetailDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewPage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class GetPageVersionQueryHandler : IRequestHandler<GetPageVersionQuery, Result<PageVersionDetailDto>>
{
    private readonly IDocumentDbContext _context;

    public GetPageVersionQueryHandler(IDocumentDbContext context) => _context = context;

    public async Task<Result<PageVersionDetailDto>> Handle(GetPageVersionQuery request, CancellationToken ct)
    {
        var pageExists = await _context.Pages.AsNoTracking()
            .AnyAsync(page => page.Id == request.PageId && page.DeletedAt == null, ct);
        if (!pageExists) throw new NotFoundException(nameof(Page), request.PageId);

        var version = await _context.DocumentVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VersionId && v.PageId == request.PageId, ct)
            ?? throw new NotFoundException(nameof(DocumentVersion), request.VersionId);

        var (title, blocks) = PageVersionSnapshot.Parse(version.Snapshot.Data!.Value);
        return Result<PageVersionDetailDto>.Success(new PageVersionDetailDto(
            DocumentDtoMapper.ToVersionDto(version),
            title,
            blocks.Select(DocumentDtoMapper.ToVersionBlockDto).ToList()));
    }
}
