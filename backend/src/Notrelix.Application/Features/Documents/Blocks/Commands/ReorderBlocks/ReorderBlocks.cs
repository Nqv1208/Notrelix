using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.Documents.Blocks.Commands.ReorderBlocks;

public record ReorderBlocksCommand(
    Guid PageId,
    List<ReorderBlockItem> Items
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public record ReorderBlockItem(Guid BlockId, string NewPosition, Guid? NewParentBlockId);

public class ReorderBlocksCommandHandler : IRequestHandler<ReorderBlocksCommand, Result>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public ReorderBlocksCommandHandler(IDocumentDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ReorderBlocksCommand request, CancellationToken ct)
    {
        var blockIds = request.Items.Select(item => item.BlockId).ToHashSet();
        var blocks = await _context.Blocks
            .Where(block => block.PageId == request.PageId && blockIds.Contains(block.Id) && !block.IsDeleted)
            .ToDictionaryAsync(block => block.Id, ct);

        var now = _dateTimeProvider.UtcNow;
        foreach (var item in request.Items)
        {
            if (!blocks.TryGetValue(item.BlockId, out var block))
                return Result.Failure(new ApplicationError("docs.block.not-found", $"Block '{item.BlockId}' was not found on page '{request.PageId}'.", ApplicationErrorType.NotFound));

            var newPosition = FractionalIndex.Create(item.NewPosition);
            if (item.NewParentBlockId is null)
            {
                block.MoveToRoot(newPosition, _currentUser.UserId, now);
            }
            else
            {
                var parentBlock = await _context.Blocks
                    .FirstOrDefaultAsync(b => b.Id == item.NewParentBlockId.Value && b.PageId == request.PageId && !b.IsDeleted, ct);
                if (parentBlock is null)
                    return Result.Failure(new ApplicationError("docs.block.parent-not-found", $"Parent block '{item.NewParentBlockId}' was not found on page '{request.PageId}'.", ApplicationErrorType.NotFound));

                var ancestorIds = new List<Guid>();
                var currentParentId = parentBlock.ParentId;
                while (currentParentId.HasValue)
                {
                    ancestorIds.Add(currentParentId.Value);
                    var ancestor = await _context.Blocks
                        .AsNoTracking()
                        .FirstOrDefaultAsync(b => b.Id == currentParentId.Value && b.PageId == request.PageId, ct);
                    currentParentId = ancestor?.ParentId;
                }

                var parentPath = BlockAncestorPath.Create(parentBlock.AccountId, parentBlock.WorkspaceId, parentBlock.PageId, parentBlock.Id, ancestorIds);
                block.MoveUnder(parentPath, newPosition, _currentUser.UserId, now);
            }
        }

        return Result.Success();
    }
}
