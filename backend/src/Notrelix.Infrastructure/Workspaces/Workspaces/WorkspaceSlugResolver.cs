using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Workspaces.Workspaces;

/// <summary>Reads the public workspace card through the owner-defined <c>ops.resolve_workspace_slug</c> function.</summary>
public sealed class WorkspaceSlugResolver : IWorkspaceSlugResolver
{
    private readonly ApplicationDbContext _context;

    public WorkspaceSlugResolver(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ResolvedWorkspaceCard?> ResolveAsync(Guid accountId, string slug, CancellationToken cancellationToken)
    {
        var rows = await _context.Database
            .SqlQuery<CardRow>($"""
                SELECT workspace_id, name, slug, description, is_personal, created_at, version, size, language, member_count
                  FROM ops.resolve_workspace_slug({accountId}, {slug})
                """)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return null;

        var r = rows[0];
        return new ResolvedWorkspaceCard(
            r.WorkspaceId, r.Name, r.Slug, r.Description, r.IsPersonal, r.CreatedAt, r.Version, r.Size, r.Language, (int)r.MemberCount);
    }

    private sealed record CardRow(
        Guid WorkspaceId, string Name, string Slug, string? Description, bool IsPersonal,
        DateTimeOffset CreatedAt, long Version, string? Size, string? Language, long MemberCount);
}
