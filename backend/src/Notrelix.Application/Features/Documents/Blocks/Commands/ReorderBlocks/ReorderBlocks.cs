using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Services;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Domain.Documents.Rules;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.ReorderBlocks;

/// <summary>
/// Moves blocks of one page, in order, as one change: it all applies or none of it does. Each item names the
/// version the editor read, the parent and the sibling it goes next to; the server makes the sort key (a client key
/// is kept only when it lies between the siblings named). An item that leaves a block where it already is changes
/// nothing, and a request that moved something announces one page save.
/// </summary>
public record ReorderBlocksCommand(
    Guid PageId,
    List<ReorderBlockItem> Items
) : ICommand<Result<List<BlockDto>>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

/// <param name="ParentBlockId">The new parent; the page root when left out.</param>
/// <param name="AfterBlockId">The sibling the block goes right after; with <paramref name="BeforeBlockId"/> also named they must be neighbours.</param>
/// <param name="BeforeBlockId">The sibling the block goes right before (to move to the top); neither named puts it last.</param>
/// <param name="Position">A client key, kept only when it lies strictly between the siblings named.</param>
public record ReorderBlockItem(Guid BlockId, long? ExpectedVersion, Guid? ParentBlockId, Guid? AfterBlockId, Guid? BeforeBlockId, string? Position);

public class ReorderBlocksCommandHandler : IRequestHandler<ReorderBlocksCommand, Result<List<BlockDto>>>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReorderBlocksCommandHandler(IDocumentDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<List<BlockDto>>> Handle(ReorderBlocksCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.FirstOrDefaultAsync(p => p.Id == request.PageId && p.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(Page), request.PageId);
        PageRules.EnsureCanEdit(page.Status);

        var live = await _context.Blocks
            .Where(b => b.PageId == request.PageId && b.DeletedAt == null)
            .ToListAsync(ct);
        var set = new PageBlockSet(_requestContext.RequireAccountId(), page.WorkspaceId, request.PageId, live);
        var userId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;

        var tracker = new BlockSaveTracker();
        var answered = new List<Block>();
        foreach (var item in request.Items)
        {
            var block = set.Find(item.BlockId) ?? throw new NotFoundException(nameof(Block), item.BlockId);
            PageBlockSet.EnsureVersion(block, item.ExpectedVersion);
            tracker.Before(block);
            set.Place(block, item.ParentBlockId, item.AfterBlockId, item.Position, userId, now, item.BeforeBlockId);
            answered.Add(block);
        }

        tracker.Complete(page, answered, userId, now);
        return Result<List<BlockDto>>.Success(answered.Select(DocumentDtoMapper.ToBlockDto).ToList());
    }
}
