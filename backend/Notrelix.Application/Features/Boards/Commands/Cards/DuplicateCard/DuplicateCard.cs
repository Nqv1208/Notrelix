using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists.DuplicateList;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.Cards.DuplicateCard;
using global::Notrelix.Application.Features.Boards.Commands.Cards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.Cards.DuplicateCard;

public record DuplicateCardCommand(Guid CardId) : IRequest<Result<Guid>>;

public class DuplicateCardCommandHandler : IRequestHandler<DuplicateCardCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DuplicateCardCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(DuplicateCardCommand request, CancellationToken ct)
    {
        var source = await _context.Cards
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CardId && !c.IsDeleted && !c.IsArchived, ct);
        if (source is null) throw new NotFoundException(nameof(Card), request.CardId);

        var nextPosition = await _context.Cards
            .Where(c => c.ListId == source.ListId && !c.IsDeleted && !c.IsArchived)
            .MaxAsync(c => (double?)c.Position, ct) + 1 ?? source.Position + 1;

        var duplicate = DuplicateListCommandHandler.CloneCard(
            source,
            source.ListId,
            _currentUser.UserId,
            $"{source.Title} copy",
            nextPosition);

        _context.Cards.Add(duplicate);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(duplicate.Id);
    }
}
