using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Documents.Pages;

/// <summary>
/// Finds the pages that mention a page by reading block content in the
/// request's data session (tenant RLS applies). The mention forms are matched
/// on the stored JSON, so nothing is duplicated into a link table: a page-reference
/// block (<c>properties.pageId</c>) and an inline <c>pageRef</c> mark in rich text;
/// <c>legacy_content</c> keeps the mentions of blocks written before rich text v1.
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
                    (b.type = 'PageReference' AND b.properties ->> 'pageId' = {target})
                 OR jsonb_path_exists(
                        b.content,
                        '$.runs[*].marks[*] ? (@.type == "pageRef" && @.pageId == $target)',
                        jsonb_build_object('target', {target}::text))
                 OR (jsonb_typeof(b.legacy_content -> 'pageMentionIds') = 'array'
                     AND (b.legacy_content -> 'pageMentionIds') ? {target})
               )
            """).ToListAsync(cancellationToken);
    }
}
