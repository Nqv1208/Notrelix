using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists.DuplicateList;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.Cards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.BoardLists.DuplicateList;

public record DuplicateListCommand(Guid ListId) : IRequest<Result<Guid>>;

public class DuplicateListCommandHandler : IRequestHandler<DuplicateListCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DuplicateListCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(DuplicateListCommand request, CancellationToken ct)
    {
        var source = await _context.BoardLists
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.ListId && !l.IsArchived, ct);
        if (source is null) throw new NotFoundException(nameof(BoardList), request.ListId);

        var nextPosition = await _context.BoardLists
            .Where(l => l.BoardId == source.BoardId && !l.IsArchived)
            .MaxAsync(l => (double?)l.Position, ct) + 1 ?? source.Position + 1;

        var duplicate = BoardList.Create(source.BoardId, $"{source.Title} copy", nextPosition, source.Color);
        _context.BoardLists.Add(duplicate);

        var cards = await _context.Cards
            .AsNoTracking()
            .Where(c => c.ListId == source.Id && !c.IsDeleted && !c.IsArchived)
            .OrderBy(c => c.Position)
            .ToListAsync(ct);

        foreach (var card in cards)
        {
            _context.Cards.Add(CloneCard(card, duplicate.Id, _currentUser.UserId, card.Title, card.Position));
        }

        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(duplicate.Id);
    }

    internal static Card CloneCard(Card source, Guid listId, Guid createdByUserId, string title, double position)
    {
        var copy = Card.Create(listId, createdByUserId, title, position);
        copy.UpdateDescription(source.DescriptionMd);
        copy.UpdatePriority(source.Priority);
        copy.UpdateStatus(source.Status);
        copy.SetDueDate(source.DueDate);
        copy.SetStartDate(source.StartDate);
        copy.UpdateCover(source.Cover);
        if (source.LinkedPageId.HasValue) copy.LinkPage(source.LinkedPageId.Value);
        copy.ReplaceFieldValues(source.FieldValues);
        return copy;
    }
}
