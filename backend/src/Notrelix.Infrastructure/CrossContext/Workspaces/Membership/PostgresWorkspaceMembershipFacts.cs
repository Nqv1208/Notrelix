using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Membership;

namespace Notrelix.Infrastructure.CrossContext.Workspaces.Membership;

/// <summary>
/// Workspaces-owned implementation of the published membership facts contract.
/// Reads the Workspaces-managed membership storage and projects the
/// authorization-relevant facts; consumers never touch Workspace tables.
/// </summary>
public sealed class PostgresWorkspaceMembershipFacts : IWorkspaceMembershipFacts
{
    private readonly IWorkspaceDbContext _context;

    public PostgresWorkspaceMembershipFacts(IWorkspaceDbContext context)
    {
        _context = context;
    }

    public async Task<WorkspaceMembershipFact?> ResolveAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var member = await _context.WorkspaceMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.AccountId == accountId
                  && m.WorkspaceId == workspaceId
                  && m.UserId == userId,
                cancellationToken);

        if (member is null)
        {
            return null;
        }

        return new WorkspaceMembershipFact(
            member.AccountId,
            member.WorkspaceId,
            member.UserId,
            IsActiveMember: member.Status == Domain.Workspaces.Members.WorkspaceMemberStatus.Active);
    }
}