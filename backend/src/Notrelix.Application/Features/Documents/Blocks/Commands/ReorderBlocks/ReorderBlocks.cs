using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.ReorderBlocks;

public record ReorderBlocksCommand(
    Guid PageId,
    List<ReorderBlockItem> Items
) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Page, PageId);
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
                return Result.Failure($"Block '{item.BlockId}' was not found on page '{request.PageId}'.");

            block.Move(item.NewParentBlockId, FractionalIndex.Create(item.NewPosition), _currentUser.UserId, now);
        }

        return Result.Success();
    }
}
