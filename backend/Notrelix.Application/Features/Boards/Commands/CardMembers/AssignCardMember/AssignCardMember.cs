using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.CardMembers.AssignCardMember;
using global::Notrelix.Application.Features.Boards.Commands.CardMembers;
using global::Notrelix.Application.Features.Boards.Commands.Cards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.CardMembers.AssignCardMember;

public record AssignCardMemberCommand(Guid CardId, Guid UserId) : IRequest<Result>;

public class AssignCardMemberCommandHandler : IRequestHandler<AssignCardMemberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public AssignCardMemberCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(AssignCardMemberCommand request, CancellationToken ct)
    {
        var card = await _context.Cards
            .Include(c => c.List)
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.CardId, ct);
        if (card is null) throw new NotFoundException(nameof(Card), request.CardId);
        await _permissions.EnsureCanEditBoardAsync(card.List.BoardId, _currentUser.UserId, ct);

        var workspaceId = await _context.Boards
            .Where(board => board.Id == card.List.BoardId)
            .Select(board => board.WorkspaceId)
            .FirstAsync(ct);

        if (!await _permissions.CanViewWorkspaceAsync(workspaceId, request.UserId, ct))
            throw new ForbiddenException("Chỉ có thể assign thành viên thuộc cùng workspace.");

        card.AssignMember(request.UserId, _currentUser.UserId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
