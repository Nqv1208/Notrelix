using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Document.Common;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Document.Commands.Blocks.ReorderBlocks;

public record ReorderBlocksCommand(
    Guid PageId,
    List<ReorderBlockItem> Items
) : IRequest<Result>;

public record ReorderBlockItem(Guid BlockId, double NewPosition, Guid? NewParentBlockId);

public class ReorderBlocksCommandHandler : IRequestHandler<ReorderBlocksCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public ReorderBlocksCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(ReorderBlocksCommand request, CancellationToken ct)
    {
        var blockIds = request.Items.Select(item => item.BlockId).ToHashSet();
        var blocks = await _context.Blocks
            .Where(block => block.PageId == request.PageId && blockIds.Contains(block.Id) && !block.IsDeleted)
            .ToDictionaryAsync(block => block.Id, ct);

        foreach (var item in request.Items)
        {
            if (!blocks.TryGetValue(item.BlockId, out var block))
                return Result.Failure($"Block '{item.BlockId}' was not found on page '{request.PageId}'.");

            block.Move(item.NewPosition, item.NewParentBlockId);
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
