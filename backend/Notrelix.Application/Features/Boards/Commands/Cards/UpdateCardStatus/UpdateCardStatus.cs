using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.Cards.UpdateCard;
using global::Notrelix.Application.Features.Boards.Commands.Cards.UpdateCardStatus;
using global::Notrelix.Application.Features.Boards.Commands.Cards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.Cards.UpdateCardStatus;

public record UpdateCardStatusCommand(Guid CardId, string Status) : IRequest<Result>;

public class UpdateCardStatusCommandHandler : IRequestHandler<UpdateCardStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public UpdateCardStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(UpdateCardStatusCommand request, CancellationToken ct)
    {
        var card = await _context.Cards
            .Include(c => c.List)
            .FirstOrDefaultAsync(c => c.Id == request.CardId, ct);
        if (card is null) throw new NotFoundException(nameof(Card), request.CardId);
        await _permissions.EnsureCanEditBoardAsync(card.List.BoardId, _currentUser.UserId, ct);
        card.ChangeStatus(Enum.Parse<Domain.Enums.CardStatus>(request.Status, ignoreCase: true), _currentUser.UserId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
