using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Domain.Governance.AccessChanges;
using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Infrastructure.Data.Authz;

/// <summary>
/// Reads <c>governance.resource_access_changes</c> through the request's data session (workspace RLS
/// applies) and names Team subjects from <c>workspace.teams</c>, the same read-only cross-schema reach
/// the access entries reader already has.
/// </summary>
public sealed class EfResourceAccessChangeReader : IResourceAccessChangeReader
{
    private readonly ApplicationDbContext _db;

    public EfResourceAccessChangeReader(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ResourceAccessChangeRow>> ReadAsync(
        Guid workspaceId,
        string resourceKind,
        Guid resourceId,
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken)
    {
        var kind = ResourceKind.Create(resourceKind);
        var rows = await _db.ResourceAccessChanges
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId && c.ResourceKind == kind && c.ResourceId == resourceId && c.RetentionUntil > now)
            .OrderByDescending(c => c.OccurredAt)
            // Within one transaction the visibility change is recorded after the member additions it follows.
            .ThenByDescending(c => c.Kind == ResourceAccessChangeKind.MadePrivate || c.Kind == ResourceAccessChangeKind.VisibilityChanged)
            .ThenByDescending(c => c.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return [];
        }

        var teamIds = rows.Where(r => r.SubjectType == "Team" && r.SubjectId.HasValue).Select(r => r.SubjectId!.Value).Distinct().ToList();
        var teamNames = teamIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Teams.AsNoTracking()
                .Where(t => t.WorkspaceId == workspaceId && teamIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var userIds = rows.Where(r => r.SubjectType == "User" && r.SubjectId.HasValue).Select(r => r.SubjectId!.Value).Distinct().ToList();
        var guests = userIds.Count == 0
            ? new HashSet<Guid>()
            : (await _db.WorkspaceMembers.AsNoTracking()
                .Where(m => m.WorkspaceId == workspaceId && userIds.Contains(m.UserId) && m.Role == WorkspaceRole.Guest && m.Status != WorkspaceMemberStatus.Removed)
                .Select(m => m.UserId)
                .ToListAsync(cancellationToken)).ToHashSet();

        return rows.Select(r => new ResourceAccessChangeRow(
            r.Id,
            r.Kind switch
            {
                ResourceAccessChangeKind.Added => AccessChangeKinds.Added,
                ResourceAccessChangeKind.Changed => AccessChangeKinds.Changed,
                ResourceAccessChangeKind.Removed => AccessChangeKinds.Removed,
                ResourceAccessChangeKind.MadePrivate => AccessChangeKinds.MadePrivate,
                _ => AccessChangeKinds.VisibilityChanged,
            },
            r.ActorUserId,
            r.SubjectType,
            r.SubjectId,
            r.SubjectType == "Team" && r.SubjectId is { } teamId ? teamNames.GetValueOrDefault(teamId) : null,
            r.SubjectType == "User" && r.SubjectId is { } userId && guests.Contains(userId),
            r.FromLevel,
            r.ToLevel,
            r.OccurredAt)).ToList();
    }
}
