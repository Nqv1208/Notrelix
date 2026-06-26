using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Documents.Common;
using global::Notrelix.Application.Features.Documents.DTOs;

namespace Notrelix.Application.Features.Documents.Pages.Queries.GetWorkspacePages;

public record GetWorkspacePagesQuery(Guid WorkspaceId) : IQuery<Result<List<PageDto>>>;

public class GetWorkspacePagesQueryHandler : IRequestHandler<GetWorkspacePagesQuery, Result<List<PageDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetWorkspacePagesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<PageDto>>> Handle(GetWorkspacePagesQuery request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces.AsNoTracking()
            .AnyAsync(workspace => workspace.Id == request.WorkspaceId && workspace.Status == WorkspaceStatus.Active && !workspace.IsDeleted, ct);
        if (!workspaceExists) throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var pageEntities = await _context.Pages.AsNoTracking()
            .Where(page => page.WorkspaceId == request.WorkspaceId && !page.IsDeleted && page.Status != PageStatus.Archived)
            .OrderBy(page => page.Title)
            .ToListAsync(ct);

        return Result<List<PageDto>>.Success(pageEntities.Select(DocumentDtoMapper.ToPageDto).ToList());
    }
}
