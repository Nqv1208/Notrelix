using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Document.Common;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Document.Commands.Blocks.BatchUpdateBlocks;

public record BatchUpdateBlocksCommand(
    Guid PageId,
    List<BatchUpdateBlockItem> Blocks
) : IRequest<Result<List<Guid>>>;

public record BatchUpdateBlockItem(
    Guid Id,
    string? Type,
    string? Properties,
    double? Position,
    Guid? ParentBlockId
);

public class BatchUpdateBlocksCommandHandler : IRequestHandler<BatchUpdateBlocksCommand, Result<List<Guid>>>
{
    private readonly IApplicationDbContext _context;
    public BatchUpdateBlocksCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<Guid>>> Handle(BatchUpdateBlocksCommand request, CancellationToken ct)
    {
        var blockIds = request.Blocks.Select(block => block.Id).ToHashSet();
        var blocks = await _context.Blocks
            .Where(block => block.PageId == request.PageId && blockIds.Contains(block.Id) && !block.IsDeleted)
            .ToDictionaryAsync(block => block.Id, ct);

        var updatedIds = new List<Guid>();
        foreach (var patch in request.Blocks)
        {
            if (!blocks.TryGetValue(patch.Id, out var block))
                return Result<List<Guid>>.Failure($"Block '{patch.Id}' was not found on page '{request.PageId}'.");

            if (patch.Type is not null) block.UpdateType(patch.Type);
            if (patch.Properties is not null) block.UpdateProperties(patch.Properties);
            if (patch.Position.HasValue || patch.ParentBlockId.HasValue) block.Move(patch.Position ?? block.Position, patch.ParentBlockId);
            updatedIds.Add(block.Id);
        }

        await _context.SaveChangesAsync(ct);
        return Result<List<Guid>>.Success(updatedIds);
    }
}
