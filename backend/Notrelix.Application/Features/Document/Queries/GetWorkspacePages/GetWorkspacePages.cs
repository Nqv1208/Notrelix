using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Document.Common;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Document.Queries.GetWorkspacePages;

public record GetWorkspacePagesQuery(Guid WorkspaceId) : IRequest<Result<List<PageDto>>>;

public class GetWorkspacePagesQueryHandler : IRequestHandler<GetWorkspacePagesQuery, Result<List<PageDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetWorkspacePagesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<PageDto>>> Handle(GetWorkspacePagesQuery request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces.AsNoTracking()
            .AnyAsync(workspace => workspace.Id == request.WorkspaceId && !workspace.IsArchived, ct);
        if (!workspaceExists) throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var pageEntities = await _context.Pages.AsNoTracking()
            .Where(page => page.WorkspaceId == request.WorkspaceId && !page.IsDeleted && !page.IsArchived)
            .OrderBy(page => page.Position)
            .ThenBy(page => page.Title)
            .ToListAsync(ct);

        return Result<List<PageDto>>.Success(pageEntities.Select(DocumentDtoMapper.ToPageDto).ToList());
    }
}
