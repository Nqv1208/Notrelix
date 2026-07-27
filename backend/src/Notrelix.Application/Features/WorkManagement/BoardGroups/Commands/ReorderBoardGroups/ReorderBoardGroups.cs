using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ReorderBoardGroups;

public record ReorderBoardGroupsCommand(Guid BoardId, List<ReorderItem> Items, string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"reorder-groups:{BoardId}";
}

public class ReorderBoardGroupsCommandHandler : IRequestHandler<ReorderBoardGroupsCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReorderBoardGroupsCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ReorderBoardGroupsCommand request, CancellationToken ct)
    {
        if (request.Items.Count == 0)
            return Result.Success();

        var itemIds = request.Items.Select(item => item.Id).ToHashSet();
        var lists = await _context.BoardGroups
            .Where(list => itemIds.Contains(list.Id))
            .ToListAsync(ct);

        if (lists.Count != itemIds.Count)
            throw new NotFoundException(nameof(BoardGroup), string.Join(",", itemIds));

        if (lists.Any(list => list.BoardId != request.BoardId))
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException("ListBoardMismatch", "All reordered groups must belong to the requested board.");

        var now = _dateTimeProvider.UtcNow;
        var positionsById = request.Items.ToDictionary(i => i.Id, i => i.NewPosition);
        var sorted = lists.OrderBy(l => positionsById[l.Id]).ToList();
        var newPositions = FractionalIndexGenerator.GenerateNKeysBetween(null, null, sorted.Count);
        for (var idx = 0; idx < sorted.Count; idx++)
        {
            sorted[idx].UpdatePosition(newPositions[idx], _currentUser.UserId, now);
        }

        return Result.Success();
    }
}
