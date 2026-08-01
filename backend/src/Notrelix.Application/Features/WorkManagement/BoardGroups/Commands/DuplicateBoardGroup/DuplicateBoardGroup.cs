using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;

public record DuplicateBoardGroupCommand(Guid GroupId, string? IdempotencyKey = null) : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardGroup, GroupId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"duplicate-group:{GroupId}";
}

public class DuplicateBoardGroupCommandHandler : IRequestHandler<DuplicateBoardGroupCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;

    public DuplicateBoardGroupCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider timeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> Handle(DuplicateBoardGroupCommand request, CancellationToken ct)
    {
        var source = await _context.BoardGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.GroupId && !l.IsDeleted, ct);
        if (source is null) throw new NotFoundException(nameof(BoardGroup), request.GroupId);

        var lastGroup = await _context.BoardGroups
            .Where(l => l.BoardId == source.BoardId && !l.IsDeleted)
            .OrderByDescending(l => l.Position)
            .FirstOrDefaultAsync(ct);

        var nextPosition = lastGroup != null
            ? FractionalIndex.Create(lastGroup.Position.Value + "1")
            : FractionalIndex.Initial();

        var board = await _context.Boards
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == source.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(Board), source.BoardId);

        var now = _timeProvider.UtcNow;

        var accountId = _requestContext.RequireAccountId();
        var duplicate = BoardGroup.Create(accountId, board.WorkspaceId, source.BoardId, $"{source.Title} copy", source.Color, nextPosition, _requestContext.UserId, now);
        _context.BoardGroups.Add(duplicate);

        var cards = await _context.BoardItems
            .AsNoTracking()
            .Where(c => c.GroupId == source.Id && !c.IsDeleted)
            .OrderBy(c => c.Position)
            .ToListAsync(ct);

        foreach (var card in cards)
        {
            _context.BoardItems.Add(CloneCard(card, accountId, duplicate.Id, board.Id, board.WorkspaceId, _requestContext.UserId, card.Name, card.Position, now));
        }

        return Result<Guid>.Success(duplicate.Id);
    }

    internal static BoardItem CloneCard(BoardItem source, Guid accountId, Guid groupId, Guid boardId, Guid workspaceId, Guid createdByUserId, string name, FractionalIndex position, DateTimeOffset createdAt)
    {
        var copy = BoardItem.CreateRoot(
            accountId,
            workspaceId,
            boardId,
            groupId,
            name,
            position,
            createdByUserId,
            createdAt,
            startedAt: source.StartedAt,
            dueAt: source.DueAt);

        return copy;
    }
}
