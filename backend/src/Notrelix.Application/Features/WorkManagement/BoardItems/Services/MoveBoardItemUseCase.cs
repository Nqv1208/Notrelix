using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>
/// Producer-local move-item use case shared by the HTTP command handler and
/// the WorkManagement Public target action. This is the single mutation
/// implementation for moving a board item between groups on the same board;
/// callers only differ in how the execution principal/scope arrives.
/// </summary>
public sealed class MoveBoardItemUseCase
{
    private readonly IWorkManagementDbContext _context;
    private readonly IDateTimeProvider _timeProvider;

    public MoveBoardItemUseCase(IWorkManagementDbContext context, IDateTimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<MoveBoardItemOutcome> MoveAsync(
        Guid itemId,
        Guid newGroupId,
        Guid executorUserId,
        CancellationToken cancellationToken)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(item => item.Id == itemId, cancellationToken);

        if (item == null)
            throw new NotFoundException("BoardItem", itemId);

        var group = await _context.BoardGroups
            .FirstOrDefaultAsync(g => g.Id == newGroupId && g.BoardId == item.BoardId, cancellationToken);

        if (group == null)
            throw new NotFoundException("BoardGroup", newGroupId);

        var now = _timeProvider.UtcNow;
        var position = FractionalIndexGenerator.GenerateKeyBetween(null, null);

        item.MoveToGroup(BoardGroupRef.From(group), position, executorUserId, now);

        var memberIds = await _context.BoardItemMembers
            .Where(m => m.ItemId == item.Id)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var labelIds = await _context.BoardItemLabels
            .Where(l => l.ItemId == item.Id)
            .Select(l => l.LabelId)
            .ToListAsync(cancellationToken);

        return new MoveBoardItemOutcome(
            new BoardItemSlimDto(
                item.Id,
                item.GroupId,
                item.Name,
                item.Position.Value,
                memberIds,
                labelIds),
            item.Version);
    }
}

/// <summary>
/// Producer-internal move outcome: the shared slim response plus the aggregate
/// version after the move (realtime stream consumers need the stream version).
/// </summary>
public sealed record MoveBoardItemOutcome(BoardItemSlimDto Item, long Version);
