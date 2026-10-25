using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Documents.Pages;

/// <summary>
/// Finds the pages that mention a page by reading block content in the
/// request's data session (tenant RLS applies). Both mention forms are matched
/// on the stored JSON, so nothing is duplicated into a link table.
/// </summary>
public sealed class PageBacklinkReader : IPageBacklinkReader
{
    private readonly ApplicationDbContext _db;

    public PageBacklinkReader(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<Guid>> FindMentioningPageIdsAsync(Guid workspaceId, Guid targetPageId, CancellationToken cancellationToken)
    {
        var target = targetPageId.ToString();
        return await _db.Database.SqlQuery<Guid>($"""
            SELECT DISTINCT b.page_id AS "Value"
              FROM docs.blocks b
             WHERE b.workspace_id = {workspaceId}
               AND b.deleted_at IS NULL
               AND b.page_id <> {targetPageId}
               AND (
                    (b.type = 'PageReference' AND b.content ->> 'linkedPageId' = {target})
                 OR (jsonb_typeof(b.content -> 'pageMentionIds') = 'array'
                     AND (b.content -> 'pageMentionIds') ? {target})
               )
            """).ToListAsync(cancellationToken);
    }
}
