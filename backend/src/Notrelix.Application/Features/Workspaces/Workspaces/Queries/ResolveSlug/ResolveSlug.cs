using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Queries.ResolveSlug;

public record ResolveSlugQuery(
    Guid AccountId,
    string Slug
) : IQuery<Result<WorkspaceDto>>, IAnonymousRequest, IReadRequest, IGlobalRequest;

public class ResolveSlugQueryHandler : IRequestHandler<ResolveSlugQuery, Result<WorkspaceDto>>
{
    private readonly IWorkspaceDbContext _context;

    public ResolveSlugQueryHandler(IWorkspaceDbContext context)
    {
        _context = context;
    }

    public async Task<Result<WorkspaceDto>> Handle(ResolveSlugQuery request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(
                w => w.AccountId == request.AccountId
                    && w.Slug == request.Slug
                    && w.Status == WorkspaceStatus.Active
                    && !w.IsDeleted,
                ct);

        if (workspace is null)
            return Result<WorkspaceDto>.Failure("Workspace not found.");

        var memberCount = await _context.WorkspaceMembers
            .AsNoTracking()
            .CountAsync(m => m.WorkspaceId == workspace.Id, ct);

        return Result<WorkspaceDto>.Success(new WorkspaceDto(
            workspace.Id,
            workspace.Name,
            workspace.Slug,
            workspace.Description,
            workspace.IsPersonal,
            "free",
            null,
            null,
            null,
            false,
            memberCount,
            workspace.CreatedAt.DateTime,
            null
        ));
    }
}
