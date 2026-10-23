using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Members.Services;

/// <summary>
/// Supplies the acting member's workspace role as an explicit fact for the Domain
/// actor-versus-target and role-cap rules. Fails closed when the actor holds no active
/// membership (the pipeline already denies that case; this keeps the handler safe on its own).
/// </summary>
internal static class WorkspaceActorRoleLookup
{
    public static async Task<WorkspaceRole> GetActiveActorRoleAsync(
        IWorkspaceDbContext context,
        Guid workspaceId,
        Guid actorId,
        CancellationToken ct)
    {
        var role = await context.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == actorId && m.Status == WorkspaceMemberStatus.Active)
            .Select(m => (WorkspaceRole?)m.Role)
            .FirstOrDefaultAsync(ct);

        return role ?? throw new ForbiddenException("You do not have permission to perform this action.");
    }
}
