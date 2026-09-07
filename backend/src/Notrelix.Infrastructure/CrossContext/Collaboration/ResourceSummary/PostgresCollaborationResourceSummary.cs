using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Public.ResourceSummary;
namespace Notrelix.Infrastructure.CrossContext.Collaboration.ResourceSummary;

/// <summary>
/// Collaboration-owned implementation of the published resource summary
/// contract. Reads Collaboration-managed comment/attachment storage and
/// projects aggregated counts per canonical resource; consumers never touch
/// Collaboration tables.
/// </summary>
public sealed class PostgresCollaborationResourceSummary : ICollaborationResourceSummary
{
    private readonly ICollaborationDbContext _context;

    public PostgresCollaborationResourceSummary(ICollaborationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyDictionary<(string Kind, Guid ResourceId), CollaborationResourceSummaryFact>> GetSummariesAsync(
        IReadOnlyCollection<(string Kind, Guid ResourceId)> resources,
        CancellationToken cancellationToken)
    {
        var summaries = new Dictionary<(string Kind, Guid ResourceId), CollaborationResourceSummaryFact>(resources.Count);
        if (resources.Count == 0)
        {
            return summaries;
        }

        var idsByKind = resources
            .GroupBy(resource => resource.Kind)
            .ToDictionary(group => group.Key, group => group.Select(r => r.ResourceId).Distinct().ToArray());

        foreach (var (kind, ids) in idsByKind)
        {
            var kindValue = ResourceKind.Create(kind);
            var commentCounts = await _context.Comments
                .AsNoTracking()
                .Where(comment => comment.Target.Kind == kindValue
                                  && ids.Contains(comment.Target.ResourceId)
                                  && comment.DeletedAt == null)
                .GroupBy(comment => comment.Target.ResourceId)
                .Select(group => new { ResourceId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.ResourceId, item => item.Count, cancellationToken);

            var attachmentCounts = await _context.Attachments
                .AsNoTracking()
                .Where(attachment => attachment.Target.Kind == kindValue
                                     && ids.Contains(attachment.Target.ResourceId))
                .GroupBy(attachment => attachment.Target.ResourceId)
                .Select(group => new { ResourceId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.ResourceId, item => item.Count, cancellationToken);

            foreach (var id in ids)
            {
                summaries[(kind, id)] = new CollaborationResourceSummaryFact(
                    id,
                    kind,
                    commentCounts.GetValueOrDefault(id),
                    attachmentCounts.GetValueOrDefault(id));
            }
        }

        return summaries;
    }
}