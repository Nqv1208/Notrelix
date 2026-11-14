using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Public.SpaceViewFacts;

namespace Notrelix.Application.Features.Chat.Notifications.Services;

/// <summary>
/// Chat's answer to "which of these spaces can this person see now": an active membership of a live space of the Workspace. One query for
/// the whole page of notifications. An archived space stays readable, so its notifications stay visible.
/// </summary>
public sealed class ChatSpaceViewFacts(IChatDbContext db) : IChatSpaceViewFacts
{
    public async Task<IReadOnlyDictionary<Guid, string?>> ViewableSpacesAsync(
        Guid workspaceId, Guid userId, IReadOnlyCollection<Guid> spaceIds, CancellationToken cancellationToken)
    {
        if (spaceIds.Count == 0)
            return new Dictionary<Guid, string?>();

        var rows = await (
            from m in db.ChatMemberships.AsNoTracking()
            join s in db.ChatSpaces.AsNoTracking() on m.SpaceId equals s.Id
            where m.UserId == userId
                  && m.WorkspaceId == workspaceId
                  && m.LeftAt == null
                  && s.WorkspaceId == workspaceId
                  && s.DeletedAt == null
                  && spaceIds.Contains(s.Id)
            select new { s.Id, s.Name, s.Kind }).ToListAsync(cancellationToken);

        return rows.ToDictionary(
            r => r.Id,
            r => r.Kind is Domain.Chat.Spaces.SpaceKind.Direct or Domain.Chat.Spaces.SpaceKind.Group ? null : (string?)r.Name);
    }
}
