using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.Cards.UpdateCard;
using global::Notrelix.Application.Features.Boards.Commands.Cards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.Cards.UpdateCard;

public record UpdateCardCommand(Guid CardId, string? Title, string? DescriptionMd, string? Priority, string? Cover, DateTime? DueDate, DateTime? StartDate) : IRequest<Result>;

public class UpdateCardCommandHandler : IRequestHandler<UpdateCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public UpdateCardCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(UpdateCardCommand request, CancellationToken ct)
    {
        var card = await _context.Cards
            .Include(c => c.List)
            .FirstOrDefaultAsync(c => c.Id == request.CardId && !c.IsDeleted, ct);
        if (card is null) throw new NotFoundException(nameof(Card), request.CardId);

        await _permissions.EnsureCanEditBoardAsync(card.List.BoardId, _currentUser.UserId, ct);

        if (request.Title is not null) card.Rename(request.Title, _currentUser.UserId);
        if (request.DescriptionMd is not null) card.UpdateDescription(request.DescriptionMd);
        if (request.Priority is not null)
            card.ChangePriority(Enum.Parse<Domain.Enums.CardPriority>(request.Priority, ignoreCase: true), _currentUser.UserId);
        if (request.Cover is not null) card.UpdateCover(request.Cover);
        if (request.DueDate.HasValue || request.StartDate.HasValue) card.SetDueDate(request.DueDate, _currentUser.UserId);
        if (request.StartDate.HasValue) card.SetStartDate(request.StartDate);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
