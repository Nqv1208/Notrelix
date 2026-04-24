using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Domain.Entities.Boardss;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Events.Board;

namespace Notrelix.Application.Features.Boardss.Commands.MoveCard;

public class MoveCardCommandHandler : IRequestHandler<MoveCardCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public MoveCardCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(MoveCardCommand request, CancellationToken cancellationToken)
    {
        var card = await _context.Cards
            .Include(c => c.List) // Lấy thông tin list cũ để publish event
            .FirstOrDefaultAsync(x => x.Id == request.CardId, cancellationToken);

        if (card == null)
            throw new NotFoundException(nameof(Card), request.CardId);

        var targetList = await _context.BoardLists
            .FirstOrDefaultAsync(x => x.Id == request.ListId, cancellationToken);

        if (targetList == null)
            throw new NotFoundException(nameof(BoardList), request.ListId);

        var oldListId = card.ListId;
        var oldPosition = card.Position;

        // Cập nhật vị trí và danh sách
        card.Move(request.ListId, request.Position);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
