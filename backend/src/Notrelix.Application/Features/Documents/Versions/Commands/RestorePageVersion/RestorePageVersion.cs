using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Services;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Domain.Documents;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.Documents.Versions.Commands.RestorePageVersion;

/// <summary>
/// Puts the page's blocks back to what an entry captured. History is never rewritten: the restore adds a new
/// entry (kind restore) and, unless the current content is already the newest entry, one before it, so the
/// content being replaced stays recoverable. The caller names the blocks and versions it saw, like a block save
/// does: if any live block changed, appeared or vanished since, nothing is restored (412) and the current blocks
/// come back so the caller can look again.
/// </summary>
public record RestorePageVersionCommand(Guid PageId, Guid VersionId, List<SeenBlockVersion> CurrentBlocks) : ICommand<Result<RestorePageVersionResultDto>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class RestorePageVersionCommandHandler : IRequestHandler<RestorePageVersionCommand, Result<RestorePageVersionResultDto>>
{
    private readonly IDocumentDbContext _context;
    private readonly IPageBlockWriteLock _writeLock;
    private readonly IPageVersionRecorder _recorder;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestorePageVersionCommandHandler(IDocumentDbContext context, IPageBlockWriteLock writeLock, IPageVersionRecorder recorder, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _writeLock = writeLock;
        _recorder = recorder;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<RestorePageVersionResultDto>> Handle(RestorePageVersionCommand request, CancellationToken ct)
    {
        await _writeLock.AcquireAsync(request.PageId, ct);
        var page = await _context.Pages
            .FirstOrDefaultAsync(p => p.Id == request.PageId && p.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(Page), request.PageId);
        if (page.Status == PageStatus.Archived)
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(DocumentRuleCodes.Documents_Page_CannotEditArchived, "An archived page cannot be restored to a version.");

        var version = await _context.DocumentVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VersionId && v.PageId == request.PageId, ct)
            ?? throw new NotFoundException(nameof(DocumentVersion), request.VersionId);
        var (_, captured) = PageVersionSnapshot.Parse(version.Snapshot.Data!.Value);

        var userId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;

        // The soft-delete filter hides deleted blocks, and a block deleted since the entry is exactly what a restore
        // brings back (its id is taken, so it cannot be created again). The tenant filter goes with it, so the
        // account and workspace of the page are stated here.
        var all = await _context.Blocks
            .IgnoreQueryFilters()
            .Where(b => b.PageId == request.PageId && b.AccountId == page.AccountId && b.WorkspaceId == page.WorkspaceId)
            .ToListAsync(ct);
        var live = all.Where(b => !b.IsDeleted).ToList();
        EnsureSeen(live, request.CurrentBlocks);

        // What is being replaced stays recoverable: capture it first unless the newest entry already holds it.
        var latest = await _context.DocumentVersions.AsNoTracking()
            .Where(v => v.PageId == request.PageId)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);
        var before = PageVersionSnapshot.Build(page.Title, live);
        if (latest is null || !PageVersionSnapshot.SameContent(latest, before))
        {
            await _recorder.AddAsync(page, live, userId, DocumentVersionKind.Automatic, $"Before restoring version {version.VersionNumber}", now, ct);
        }

        var set = new PageBlockSet(page.AccountId, page.WorkspaceId, page.Id, live);
        var byId = all.ToDictionary(b => b.Id);
        var keep = captured.Select(block => block.Id).ToHashSet();

        // Parents come before their children in a capture, so each block's parent is already in place.
        foreach (var block in captured)
        {
            var content = PageBlockSet.ContentOf(JsonDocumentElement(block.Content));
            var properties = PageBlockSet.PropertiesOf(JsonDocumentElement(block.Properties));

            if (!byId.TryGetValue(block.Id, out var existing))
            {
                var position = FractionalIndex.Create(block.Position);
                existing = block.ParentId is { } parent
                    ? Block.CreateChild(page.AccountId, page.WorkspaceId, page.Id, block.Type, content!, position, userId, now, set.PathTo(parent), properties, block.Id)
                    : Block.CreateRoot(page.AccountId, page.WorkspaceId, page.Id, block.Type, content!, position, userId, now, properties, block.Id);
                _context.Blocks.Add(existing);
                set.Add(existing);
                continue;
            }

            if (existing.IsDeleted)
            {
                existing.Restore(userId, now);
                set.Add(existing);
            }

            existing.Edit(block.Type, content, properties, userId, now);
            set.PlaceAtCapturedKey(existing, block.ParentId, block.Position, userId, now);
        }

        foreach (var extra in live.Where(b => !keep.Contains(b.Id)))
        {
            extra.Delete(userId, now);
            set.Remove(extra);
        }

        var restored = await _recorder.AddAsync(
            page, set.All, userId, DocumentVersionKind.Restore, $"Restored from version {version.VersionNumber}", now, ct);
        page.RecordBlocksSaved(userId, now, []);

        var blocks = PageVersionSnapshot.InTreeOrder(set.All)
            .Select(ordered => DocumentDtoMapper.ToBlockDto(set.Find(ordered.Id)!))
            .ToList();
        return Result<RestorePageVersionResultDto>.Success(new RestorePageVersionResultDto(DocumentDtoMapper.ToVersionDto(restored), blocks));
    }

    private static System.Text.Json.JsonElement? JsonDocumentElement(string json)
    {
        using var document = System.Text.Json.JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static void EnsureSeen(IReadOnlyList<Block> live, IReadOnlyList<SeenBlockVersion> seen)
    {
        var seenById = seen.GroupBy(s => s.BlockId).ToDictionary(g => g.Key, g => g.Last().Version);
        var current = live.All(block => seenById.TryGetValue(block.Id, out var version) && version == block.Version)
            && seenById.Count == live.Count;
        if (current) return;

        throw new PreconditionFailedException(
            "The page changed since you looked at it. Reload it and try again.",
            "documents.version.restore-conflict")
        {
            Extensions = new Dictionary<string, object?>
            {
                ["currentBlocks"] = live.Select(DocumentDtoMapper.ToBlockDto).ToList(),
            },
        };
    }
}
