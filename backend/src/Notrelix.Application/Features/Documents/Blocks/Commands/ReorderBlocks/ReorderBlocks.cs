using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Documents.Blocks.Commands.ReorderBlocks;

public record ReorderBlocksCommand(
    Guid PageId,
    List<ReorderBlockItem> Items
) : ICommand<Result>, ITransactionalRequest;

public record ReorderBlockItem(Guid BlockId, string NewPosition, Guid? NewParentBlockId);

public class ReorderBlocksCommandHandler : IRequestHandler<ReorderBlocksCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public ReorderBlocksCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
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
