using Notrelix.Application.Features.Identity.Public.DeliveryAddresses;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Identity.Users;

/// <summary>
/// Identity-owned implementation of the delivery address fact for system senders. It reads under the worker role (which may read users), and
/// only for the people the sender names: an active user who is an active, non-guest member of the Workspace.
/// </summary>
public sealed class MemberDeliveryAddresses(ApplicationDbContext context) : IMemberDeliveryAddresses
{
    public async Task<IReadOnlyDictionary<Guid, string>> FindAsync(
        Guid workspaceId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, string>();

        var ids = userIds.Distinct().ToList();
        var rows = await (
            from u in context.Users.AsNoTracking()
            join m in context.WorkspaceMembers.AsNoTracking() on u.Id equals m.UserId
            where m.WorkspaceId == workspaceId
                  && ids.Contains(u.Id)
                  && m.Status == WorkspaceMemberStatus.Active
                  && m.Role != WorkspaceRole.Guest
                  && u.Status == UserStatus.Active
            select new { u.Id, u.NormalizedEmail }).ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.Id, r => r.NormalizedEmail);
    }
}
