using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;

public record DuplicateBoardGroupCommand(Guid GroupId) : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardGroup, GroupId);
}

public class DuplicateBoardGroupCommandHandler : IRequestHandler<DuplicateBoardGroupCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;
    private readonly ICurrentTenantContext _tenant;

    public DuplicateBoardGroupCommandHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider timeProvider, ICurrentTenantContext tenant)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _tenant = tenant;
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

        var accountId = _tenant.RequireAccountId();
        var duplicate = BoardGroup.Create(accountId, board.WorkspaceId, source.BoardId, $"{source.Title} copy", source.Color, nextPosition, _currentUser.UserId, now);
        _context.BoardGroups.Add(duplicate);

        var cards = await _context.BoardItems
            .AsNoTracking()
            .Where(c => c.GroupId == source.Id && !c.IsDeleted)
            .OrderBy(c => c.Position)
            .ToListAsync(ct);

        foreach (var card in cards)
        {
            _context.BoardItems.Add(CloneCard(card, accountId, duplicate.Id, board.Id, board.WorkspaceId, _currentUser.UserId, card.Name, card.Position, now));
        }

        return Result<Guid>.Success(duplicate.Id);
    }

    internal static BoardItem CloneCard(BoardItem source, Guid accountId, Guid groupId, Guid boardId, Guid workspaceId, Guid createdByUserId, string name, FractionalIndex position, DateTimeOffset createdAt)
    {
        var copy = BoardItem.Create(
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
