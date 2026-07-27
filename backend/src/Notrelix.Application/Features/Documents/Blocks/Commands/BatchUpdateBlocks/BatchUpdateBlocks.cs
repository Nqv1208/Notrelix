using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.Documents.Blocks.Commands.BatchUpdateBlocks;

public record BatchUpdateBlocksCommand(
    Guid PageId,
    List<BatchUpdateBlockItem> Blocks
) : ICommand<Result<List<Guid>>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Page, PageId);
}

public record BatchUpdateBlockItem(
    Guid Id,
    string? Type,
    string? Properties,
    string? Position,
    Guid? ParentBlockId
);

public class BatchUpdateBlocksCommandHandler : IRequestHandler<BatchUpdateBlocksCommand, Result<List<Guid>>>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public BatchUpdateBlocksCommandHandler(IDocumentDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<List<Guid>>> Handle(BatchUpdateBlocksCommand request, CancellationToken ct)
    {
        var blockIds = request.Blocks.Select(block => block.Id).ToHashSet();
        var blocks = await _context.Blocks
            .Where(block => block.PageId == request.PageId && blockIds.Contains(block.Id) && !block.IsDeleted)
            .ToDictionaryAsync(block => block.Id, ct);

        var now = _dateTimeProvider.UtcNow;
        var updatedIds = new List<Guid>();
        foreach (var patch in request.Blocks)
        {
            if (!blocks.TryGetValue(patch.Id, out var block))
                return Result<List<Guid>>.Failure($"Block '{patch.Id}' was not found on page '{request.PageId}'.");

            if (patch.Properties is not null)
                block.UpdateProperties(BlockProperties.Create(JsonValue.Create(patch.Properties)), _currentUser.UserId, now);
            if (patch.Position is not null || patch.ParentBlockId is not null)
            {
                var newPosition = patch.Position is not null ? FractionalIndex.Create(patch.Position) : block.Position;
                if (patch.ParentBlockId is null)
                {
                    block.MoveToRoot(newPosition, _currentUser.UserId, now);
                }
                else
                {
                    var parentBlock = await _context.Blocks
                        .FirstOrDefaultAsync(b => b.Id == patch.ParentBlockId.Value && b.PageId == request.PageId && !b.IsDeleted, ct);
                    if (parentBlock is null)
                        return Result<List<Guid>>.Failure($"Parent block '{patch.ParentBlockId}' was not found on page '{request.PageId}'.");

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
            updatedIds.Add(block.Id);
        }

        return Result<List<Guid>>.Success(updatedIds);
    }
}
