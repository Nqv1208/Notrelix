using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Domain.Documents.Versions;

namespace Notrelix.Application.Features.Documents.Versions.Services;

/// <summary>
/// Adds history entries (DCT-016/017). A history entry captures the page's live content as it is when it is
/// added; it never changes the page's or a block's concurrency version. The caller's unit of work commits it.
/// </summary>
public interface IPageVersionRecorder
{
    /// <summary>The longest a page goes between automatic entries while it is being edited.</summary>
    static readonly TimeSpan AutomaticInterval = TimeSpan.FromMinutes(10);

    /// <summary>Adds an entry holding exactly <paramref name="liveBlocks"/> (the content the caller has just written, not yet committed).</summary>
    Task<DocumentVersion> AddAsync(
        Page page, IEnumerable<Block> liveBlocks, Guid actorId, DocumentVersionKind kind, string? name, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Adds an automatic entry for the page's committed content when it has changed since the last entry and the
    /// last entry is at least <see cref="AutomaticInterval"/> old (or there is none). Returns null when there is nothing to capture yet.
    /// </summary>
    Task<DocumentVersion?> CaptureAutomaticAsync(Guid pageId, Guid actorId, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed class PageVersionRecorder : IPageVersionRecorder
{
    private readonly IDocumentDbContext _context;
    private readonly List<DocumentVersion> _added = [];

    public PageVersionRecorder(IDocumentDbContext context) => _context = context;

    public async Task<DocumentVersion> AddAsync(
        Page page, IEnumerable<Block> liveBlocks, Guid actorId, DocumentVersionKind kind, string? name, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var json = PageVersionSnapshot.Build(page.Title, liveBlocks);
        return await AddJsonAsync(page, json, actorId, kind, name, now, cancellationToken);
    }

    public async Task<DocumentVersion?> CaptureAutomaticAsync(Guid pageId, Guid actorId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var page = await _context.Pages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == pageId && p.DeletedAt == null, cancellationToken);
        if (page is null) return null;

        var latest = await _context.DocumentVersions.AsNoTracking()
            .Where(v => v.PageId == pageId)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is not null && now - latest.CreatedAt < IPageVersionRecorder.AutomaticInterval) return null;

        var live = await _context.Blocks.AsNoTracking()
            .Where(b => b.PageId == pageId && b.DeletedAt == null)
            .ToListAsync(cancellationToken);
        if (live.Count == 0 && latest is null) return null;

        var json = PageVersionSnapshot.Build(page.Title, live);
        if (latest is not null && PageVersionSnapshot.SameContent(latest, json)) return null;

        return await AddJsonAsync(page, json, actorId, DocumentVersionKind.Automatic, null, now, cancellationToken);
    }

    private async Task<DocumentVersion> AddJsonAsync(
        Page page, string json, Guid actorId, DocumentVersionKind kind, string? name, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var stored = await _context.DocumentVersions.AsNoTracking()
            .Where(v => v.PageId == page.Id)
            .MaxAsync(v => (int?)v.VersionNumber, cancellationToken);
        // Entries this recorder added earlier in the same unit of work are not stored yet but already hold their numbers.
        var pending = _added.Where(v => v.PageId == page.Id).Select(v => (int?)v.VersionNumber).Max();
        var last = Math.Max(stored ?? 0, pending ?? 0);

        var version = DocumentVersion.Create(
            page.AccountId, page.WorkspaceId, page.Id, last + 1,
            DocumentSnapshot.Create(JsonValue.Create(json)), actorId, now, name, kind);
        _context.DocumentVersions.Add(version);
        _added.Add(version);
        return version;
    }
}
