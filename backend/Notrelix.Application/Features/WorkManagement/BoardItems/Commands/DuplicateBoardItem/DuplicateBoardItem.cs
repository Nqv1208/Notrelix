using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem;

public record DuplicateBoardItemCommand(Guid BoardItemId) : ICommand<Result<Guid>>, ITransactionalRequest;

public class DuplicateBoardItemCommandHandler : IRequestHandler<DuplicateBoardItemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;

    public DuplicateBoardItemCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> Handle(DuplicateBoardItemCommand request, CancellationToken ct)
    {
        var source = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId && !c.IsDeleted, ct);
        if (source is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var lastItem = await _context.BoardItems
            .Where(c => c.GroupId == source.GroupId && !c.IsDeleted)
            .OrderByDescending(c => c.Position)
            .FirstOrDefaultAsync(ct);

        var nextPosition = lastItem != null
            ? FractionalIndex.Create(lastItem.Position.Value + "1")
            : FractionalIndex.Initial();

        var now = _timeProvider.UtcNow;

        var duplicate = DuplicateBoardGroupCommandHandler.CloneCard(
            source,
            source.GroupId,
            source.BoardId,
            source.WorkspaceId,
            _currentUser.UserId,
            $"{source.Name} copy",
            nextPosition,
            now);

        _context.BoardItems.Add(duplicate);
        return Result<Guid>.Success(duplicate.Id);
    }
}
