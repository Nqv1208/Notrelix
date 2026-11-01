using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Workspaces.Members.Services;

public sealed class WorkspaceMemberFacts : IWorkspaceMemberFacts
{
    private readonly IWorkspaceDbContext _context;

    public WorkspaceMemberFacts(IWorkspaceDbContext context) => _context = context;

    public async Task<WorkspaceMemberFact?> FindAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var member = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.Status != WorkspaceMemberStatus.Removed)
            .Select(m => new { m.Role, m.Status })
            .FirstOrDefaultAsync(cancellationToken);

        return member is null
            ? null
            : new WorkspaceMemberFact(userId, member.Role.ToString(), member.Status == WorkspaceMemberStatus.Active);
    }
}
