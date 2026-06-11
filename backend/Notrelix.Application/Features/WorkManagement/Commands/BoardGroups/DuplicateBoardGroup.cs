using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record DuplicateBoardGroupCommand(Guid GroupId) : IRequest<Result<Guid>>;

public class DuplicateBoardGroupCommandHandler : IRequestHandler<DuplicateBoardGroupCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DuplicateBoardGroupCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(DuplicateBoardGroupCommand request, CancellationToken ct)
    {
        var source = await _context.BoardGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.GroupId && !l.IsArchived, ct);
        if (source is null) throw new NotFoundException(nameof(BoardGroup), request.GroupId);

        var nextPosition = await _context.BoardGroups
            .Where(l => l.BoardId == source.BoardId && !l.IsArchived)
            .MaxAsync(l => (double?)l.Position, ct) + 1 ?? source.Position + 1;

        var board = await _context.Boards
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == source.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(Board), source.BoardId);

        var duplicate = BoardGroup.Create(source.BoardId, $"{source.Title} copy", nextPosition, source.Color);
        _context.BoardGroups.Add(duplicate);

        var cards = await _context.BoardItems
            .AsNoTracking()
            .Where(c => c.GroupId == source.Id && !c.IsDeleted && !c.IsArchived)
            .OrderBy(c => c.Position)
            .ToListAsync(ct);

        foreach (var card in cards)
        {
            _context.BoardItems.Add(CloneCard(card, duplicate.Id, board.Id, board.WorkspaceId, _currentUser.UserId, card.Title, card.Position));
        }

        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(duplicate.Id);
    }

    internal static BoardItem CloneCard(BoardItem source, Guid listId, Guid boardId, Guid workspaceId, Guid createdByUserId, string title, double position)
    {
        var copy = BoardItem.Create(listId, boardId, workspaceId, createdByUserId, title, position);
        copy.UpdateDescription(source.DescriptionMd);
        copy.UpdatePriority(source.Priority);
        copy.UpdateStatus(source.Status);
        copy.SetDueDate(source.DueDate);
        copy.SetStartDate(source.StartDate);
        copy.UpdateCover(source.Cover);
        if (source.LinkedPageId.HasValue) copy.LinkPage(source.LinkedPageId.Value);
        copy.ReplaceFieldValues(source.ValuesJson);
        return copy;
    }
}
