using Notrelix.Application.Features.Collaboration.Activity.Ports;
using Notrelix.Infrastructure.Data.Abstractions;

namespace Notrelix.Infrastructure.Data.Collaboration;

/// <summary>Reads one resource's history from the activity projection, newest first, one page at a time.</summary>
public sealed class ResourceActivityReader : IResourceActivityReader
{
    private readonly IActivityProjectionDbContext _context;

    public ResourceActivityReader(IActivityProjectionDbContext context)
    {
        _context = context;
    }

    public async Task<ResourceActivityPage> ListAsync(
        Guid workspaceId, string resourceKind, Guid resourceId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var rows = _context.WorkspaceActivityLogs
            .AsNoTracking()
            .Where(r => r.WorkspaceId == workspaceId
                        && r.ResourceKind == resourceKind
                        && r.ResourceId == resourceId
                        && r.DeletedAt == null);

        var total = await rows.CountAsync(cancellationToken);
        var pageRows = await rows
            .OrderByDescending(r => r.OccurredAt)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new { r.Id, r.ActorUserId, r.ActivityType, r.Title, r.ResourceDisplayName, r.DataJson, r.OccurredAt })
            .ToListAsync(cancellationToken);

        return new ResourceActivityPage(
            pageRows.Select(r => new ResourceActivityRow(
                r.Id, r.ActorUserId, r.ActivityType, r.Title, r.ResourceDisplayName,
                r.DataJson.RootElement.GetRawText(), r.OccurredAt)).ToList(),
            total);
    }
}
