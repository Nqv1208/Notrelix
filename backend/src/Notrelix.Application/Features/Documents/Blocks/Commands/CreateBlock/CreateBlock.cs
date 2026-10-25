using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.Documents.Blocks.Commands.CreateBlock;

public record CreateBlockCommand(
    Guid PageId,
    BlockType Type,
    string Properties,
    Guid? AfterBlockId,
    Guid? ParentBlockId
) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class CreateBlockCommandHandler : IRequestHandler<CreateBlockCommand, Result<Guid>>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBlockCommandHandler(IDocumentDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateBlockCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PageId && p.DeletedAt == null, ct);
        if (page is null) throw new NotFoundException(nameof(Page), request.PageId);

        var content = BlockContent.Create(JsonValue.Create(request.Properties ?? "{}"));
        var position = await PositionAsync(request, ct);
        var accountId = _requestContext.RequireAccountId();

        Block block;
        if (request.ParentBlockId.HasValue)
        {
            var parentBlock = await _context.Blocks.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == request.ParentBlockId.Value && b.DeletedAt == null, ct);
            if (parentBlock is null) throw new NotFoundException(nameof(Block), request.ParentBlockId.Value);

            var ancestorIds = await _context.Blocks.AsNoTracking()
                .Where(b => b.PageId == request.PageId && b.DeletedAt == null)
                .Select(b => new { b.Id, b.ParentId })
                .ToListAsync(ct);

            var ancestors = new List<Guid>();
            var current = parentBlock.ParentId;
            while (current.HasValue)
            {
                ancestors.Insert(0, current.Value);
                current = ancestorIds.FirstOrDefault(a => a.Id == current.Value)?.ParentId;
            }

            var parentPath = BlockAncestorPath.Create(accountId, page.WorkspaceId, request.PageId, parentBlock.Id, ancestors);
            block = Block.CreateChild(accountId, page.WorkspaceId, request.PageId, request.Type, content, position, _requestContext.UserId, _dateTimeProvider.UtcNow, parentPath);
        }
        else
        {
            block = Block.CreateRoot(accountId, page.WorkspaceId, request.PageId, request.Type, content, position, _requestContext.UserId, _dateTimeProvider.UtcNow);
        }

        _context.Blocks.Add(block);
        return Result<Guid>.Success(block.Id);
    }

    /// <summary>
    /// The new block goes right after <c>AfterBlockId</c> among its siblings, or last when none is named.
    /// Keys compare ordinally, so the siblings are ordered here rather than by the column collation.
    /// </summary>
    private async Task<FractionalIndex> PositionAsync(CreateBlockCommand request, CancellationToken ct)
    {
        var siblings = (await _context.Blocks.AsNoTracking()
                .Where(b => b.PageId == request.PageId && b.DeletedAt == null && b.ParentId == request.ParentBlockId)
                .Select(b => new { b.Id, Position = b.Position })
                .ToListAsync(ct))
            .OrderBy(b => b.Position.Value, StringComparer.Ordinal)
            .ToList();

        if (request.AfterBlockId is not { } afterId)
        {
            return FractionalIndexGenerator.GenerateKeyBetween(siblings.LastOrDefault()?.Position, null);
        }

        var index = siblings.FindIndex(b => b.Id == afterId);
        if (index < 0) throw new NotFoundException(nameof(Block), afterId);
        return FractionalIndexGenerator.GenerateKeyBetween(
            siblings[index].Position,
            index + 1 < siblings.Count ? siblings[index + 1].Position : null);
    }
}
