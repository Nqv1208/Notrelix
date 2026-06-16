using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.Documents.Blocks;
using global::Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.Document.Commands.Blocks.BatchUpdateBlocks;

public record BatchUpdateBlocksCommand(
    Guid PageId,
    List<BatchUpdateBlockItem> Blocks
) : IRequest<Result<List<Guid>>>;

public record BatchUpdateBlockItem(
    Guid Id,
    string? Type,
    string? Properties,
    string? Position,
    Guid? ParentBlockId
);

public class BatchUpdateBlocksCommandHandler : IRequestHandler<BatchUpdateBlocksCommand, Result<List<Guid>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    public BatchUpdateBlocksCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
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
                block.Move(patch.ParentBlockId, newPosition, _currentUser.UserId, now);
            }
            updatedIds.Add(block.Id);
        }

        await _context.SaveChangesAsync(ct);
        return Result<List<Guid>>.Success(updatedIds);
    }
}
