using System.Text.Json;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Services;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Application.Features.Documents.DTOs;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.BatchUpdateBlocks;

/// <summary>
/// Saves several blocks of one page as one change: it all applies or none of it does (the data session
/// owns the transaction; a failing operation throws before anything commits). An <c>upsert</c> creates
/// the block with the caller's id or edits the existing one against its <c>ExpectedVersion</c>; a
/// <c>delete</c> removes the block (and the blocks under it) against its version. Operations apply in order,
/// so a later one may place itself after a block an earlier one created.
/// </summary>
public record BatchUpdateBlocksCommand(
    Guid PageId,
    List<BatchUpdateBlockItem> Blocks
) : ICommand<Result<List<BlockDto>>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public static class BatchBlockOperations
{
    public const string Upsert = "upsert";
    public const string Delete = "delete";
    public const int MaxOperations = 500;
}

/// <param name="Op"><c>upsert</c> (default) or <c>delete</c>.</param>
/// <param name="Position">An explicit fractional key; otherwise <c>AfterBlockId</c> places the block, otherwise it goes last.</param>
/// <param name="AfterBlockId">The sibling the block goes right after under <c>ParentId</c>.</param>
/// <param name="ParentId">The parent block; placement under the page root when it is left out while a position or after-block is sent.</param>
public record BatchUpdateBlockItem(
    string? Op,
    Guid Id,
    string? Type,
    JsonElement? Content,
    JsonElement? Properties,
    string? Position,
    Guid? AfterBlockId,
    Guid? ParentId,
    long? ExpectedVersion
)
{
    public bool IsDelete => string.Equals(Op, BatchBlockOperations.Delete, StringComparison.OrdinalIgnoreCase);

    public bool RequestsPlacement => !string.IsNullOrWhiteSpace(Position) || AfterBlockId is not null || ParentId is not null;
}

public class BatchUpdateBlocksCommandHandler : IRequestHandler<BatchUpdateBlocksCommand, Result<List<BlockDto>>>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BatchUpdateBlocksCommandHandler(IDocumentDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<List<BlockDto>>> Handle(BatchUpdateBlocksCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PageId && p.DeletedAt == null, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        var accountId = _requestContext.RequireAccountId();
        var userId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;

        var live = await _context.Blocks
            .Where(b => b.PageId == request.PageId && b.DeletedAt == null)
            .ToListAsync(ct);
        var set = new PageBlockSet(accountId, page.WorkspaceId, request.PageId, live);

        var newIds = request.Blocks
            .Where(item => !item.IsDelete && set.Find(item.Id) is null)
            .Select(item => item.Id)
            .ToList();
        if (newIds.Count > 0)
        {
            var taken = await _context.Blocks
                .Where(b => newIds.Contains(b.Id))
                .Select(b => b.Id)
                .FirstOrDefaultAsync(ct);
            if (taken != Guid.Empty)
                throw new ConflictException($"Block '{taken}' already exists.", "documents.block.id-taken");
        }

        var saved = new List<Block>();
        foreach (var item in request.Blocks)
        {
            if (item.IsDelete)
            {
                Delete(set, item, userId, now);
                continue;
            }

            var type = item.Type is { Length: > 0 } name ? PageBlockSet.ParseType(name) : (BlockType?)null;
            var content = PageBlockSet.ContentOf(item.Content);
            var properties = PageBlockSet.PropertiesOf(item.Properties);

            var block = set.Find(item.Id);
            if (block is null)
            {
                if (type is null) throw new Notrelix.Application.Common.Exceptions.ValidationException($"A new block '{item.Id}' needs its type.");
                block = Create(set, item, type.Value, content, properties, userId, now);
                _context.Blocks.Add(block);
                set.Add(block);
            }
            else
            {
                PageBlockSet.EnsureVersion(block, item.ExpectedVersion);
                block.Edit(type, content, properties, userId, now);
                if (item.RequestsPlacement)
                    set.Place(block, item.ParentId, item.AfterBlockId, item.Position, userId, now);
            }

            if (!saved.Contains(block)) saved.Add(block);
        }

        return Result<List<BlockDto>>.Success(saved
            .Where(block => !block.IsDeleted)
            .Select(DocumentDtoMapper.ToBlockDto)
            .ToList());
    }

    private static Block Create(PageBlockSet set, BatchUpdateBlockItem item, BlockType type, BlockContent? content, BlockProperties? properties, Guid userId, DateTimeOffset now)
    {
        var position = set.PositionFor(item.ParentId, item.AfterBlockId, item.Position, null);
        content ??= BlockContent.Create(JsonValue.EmptyObject());
        return item.ParentId is { } parent
            ? Block.CreateChild(set.AccountId, set.WorkspaceId, set.PageId, type, content, position, userId, now, set.PathTo(parent), properties, item.Id)
            : Block.CreateRoot(set.AccountId, set.WorkspaceId, set.PageId, type, content, position, userId, now, properties, item.Id);
    }

    private static void Delete(PageBlockSet set, BatchUpdateBlockItem item, Guid userId, DateTimeOffset now)
    {
        var block = set.Find(item.Id) ?? throw new NotFoundException(nameof(Block), item.Id);
        PageBlockSet.EnsureVersion(block, item.ExpectedVersion);

        // The blocks under a deleted block go with it, so no live block is left under a deleted parent.
        var doomed = new List<Block> { block };
        for (var i = 0; i < doomed.Count; i++)
            doomed.AddRange(set.Siblings(doomed[i].Id));

        foreach (var victim in doomed)
        {
            victim.Delete(userId, now);
            set.Remove(victim);
        }
    }
}
