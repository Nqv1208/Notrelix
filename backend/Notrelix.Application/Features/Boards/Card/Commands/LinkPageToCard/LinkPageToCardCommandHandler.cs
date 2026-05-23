using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Domain.Entities.Boards;
using Notrelix.Domain.Entities.Document;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Events.Board;

namespace Notrelix.Application.Features.Boards.Board.Commands.LinkPageToCard;

public record LinkPageToCardCommand(Guid CardId, Guid PageId) : IRequest<Unit>;
public class LinkPageToCardCommandHandler : IRequestHandler<LinkPageToCardCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public LinkPageToCardCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(LinkPageToCardCommand request, CancellationToken cancellationToken)
    {
        var card = await _context.Cards
            .Include(c => c.List)
            .FirstOrDefaultAsync(x => x.Id == request.CardId, cancellationToken);

        if (card == null)
            throw new NotFoundException(nameof(Card), request.CardId);

        var page = await _context.Pages
            .FirstOrDefaultAsync(x => x.Id == request.PageId, cancellationToken);

        if (page == null)
            throw new NotFoundException(nameof(Page), request.PageId);

        // Map link
        card.LinkPage(request.PageId);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
