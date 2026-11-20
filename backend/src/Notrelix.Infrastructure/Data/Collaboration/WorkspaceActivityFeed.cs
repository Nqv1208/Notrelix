using Notrelix.Application.Features.Collaboration.Activity;
using Notrelix.Application.Features.Collaboration.Public.WorkspaceActivity;
using Notrelix.Infrastructure.Data.Abstractions;

namespace Notrelix.Infrastructure.Data.Collaboration;

/// <summary>Reads the workspace's activity rows from the activity projection, newest first, by keyset on (time, id).</summary>
public sealed class WorkspaceActivityFeed : IWorkspaceActivityFeed
{
    private readonly IActivityProjectionDbContext _context;

    public WorkspaceActivityFeed(IActivityProjectionDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<WorkspaceActivityFact>> ListAsync(
        Guid workspaceId, WorkspaceActivityPosition? after, int take, CancellationToken cancellationToken)
    {
        var rows = _context.WorkspaceActivityLogs
            .AsNoTracking()
            .Where(r => r.WorkspaceId == workspaceId && r.DeletedAt == null);

        if (after is { } position)
        {
            var at = position.OccurredAt;
            var id = position.Id;
            rows = rows.Where(r => r.OccurredAt < at || (r.OccurredAt == at && r.Id.CompareTo(id) < 0));
        }

        var page = await rows
            .OrderByDescending(r => r.OccurredAt)
            .ThenByDescending(r => r.Id)
            .Take(take)
            .Select(r => new
            {
                r.Id,
                r.SourceContext,
                r.ActivityType,
                r.ActorUserId,
                r.Title,
                r.SubjectType,
                r.SubjectId,
                r.SubjectDisplayName,
                r.TargetType,
                r.TargetId,
                r.ResourceKind,
                r.ResourceId,
                r.OccurredAt,
            })
            .ToListAsync(cancellationToken);

        return page.Select(r => new WorkspaceActivityFact(
            r.Id, r.ActivityType, r.ActorUserId, r.Title, r.SubjectType, r.SubjectId, r.SubjectDisplayName,
            WorkspaceActivityScopes.Of(r.SourceContext, r.SubjectType, r.SubjectId, r.TargetType, r.TargetId, r.ResourceKind, r.ResourceId),
            r.OccurredAt)).ToList();
    }
}
