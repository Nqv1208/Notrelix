using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Infrastructure.Services;

public sealed class WorkspaceAccessChecker : IWorkspaceAccessChecker
{
    private readonly IApplicationDbContext _context;

    public WorkspaceAccessChecker(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> EnsureWorkspaceExistsAsync(Guid workspaceId, CancellationToken ct)
    {
        var exists = await _context.Workspaces.AnyAsync(w => w.Id == workspaceId, ct);
        return exists
            ? Result.Success()
            : Result.Failure($"Workspace {workspaceId} not found");
    }

    public async Task<Result> EnsureWorkspaceIsActiveAsync(Guid workspaceId, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .Where(w => w.Id == workspaceId)
            .Select(w => new { w.Status })
            .FirstOrDefaultAsync(ct);

        if (workspace is null)
            return Result.Failure($"Workspace {workspaceId} not found");

        return workspace.Status == WorkspaceStatus.Active
            ? Result.Success()
            : Result.Failure($"Workspace {workspaceId} is not active (current status: {workspace.Status})");
    }
}
