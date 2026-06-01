using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Queries.GetWorkspace;

public record GetWorkspaceQuery(Guid WorkspaceId) : IRequest<Result<WorkspaceDto>>;

public class GetWorkspaceQueryHandler : IRequestHandler<GetWorkspaceQuery, Result<WorkspaceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetWorkspaceQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkspaceDto>> Handle(GetWorkspaceQuery request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && !w.IsArchived, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == _currentUser.UserId, ct);

        if (!isMember)
            throw new ForbiddenException("Bạn không phải thành viên của workspace này.");

        var memberCount = await _context.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspace.Id, ct);

        return Result<WorkspaceDto>.Success(new WorkspaceDto(
            workspace.Id,
            workspace.Name,
            workspace.Slug,
            workspace.Description,
            workspace.IsPersonal,
            workspace.Plan.ToString(),
            workspace.Icon.Type.ToString(),
            workspace.Icon.Value,
            workspace.CoverUrl,
            workspace.IsArchived,
            memberCount,
            workspace.CreatedAt
        ));
    }
}
