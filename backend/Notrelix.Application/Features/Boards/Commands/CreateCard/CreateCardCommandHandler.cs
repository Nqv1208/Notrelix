using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Domain.Entities.Boardss;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Events.Board;

namespace Notrelix.Application.Features.Boardss.Commands.CreateCard;

public class CreateCardCommandHandler : IRequestHandler<CreateCardCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateCardCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCardCommand request, CancellationToken cancellationToken)
    {
        var list = await _context.BoardLists
            .FirstOrDefaultAsync(x => x.Id == request.ListId, cancellationToken);

        if (list == null)
            throw new NotFoundException(nameof(BoardList), request.ListId);

        // Tính toán position (mặc định đặt ở cuối danh sách)
        var maxPosition = await _context.Cards
            .Where(x => x.ListId == request.ListId && !x.IsDeleted)
            .MaxAsync(x => (double?)x.Position, cancellationToken) ?? 0;

        var newPosition = maxPosition + 65536.0; // Khoảng cách an toàn ban đầu

        var card = Card.Create(
            listId: request.ListId,
            createdBy: _currentUser.UserId,
            title: request.Title,
            position: newPosition
        );

        // Thêm domain event
        card.AddDomainEvent(new CardCreatedEvent(card.Id, list.BoardId, list.Id, card.Title, card.CreatedByUserId));

        _context.Cards.Add(card);
        await _context.SaveChangesAsync(cancellationToken);

        return card.Id;
    }
}
