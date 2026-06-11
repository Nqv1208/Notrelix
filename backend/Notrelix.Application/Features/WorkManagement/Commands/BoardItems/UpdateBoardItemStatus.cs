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

public record UpdateBoardItemStatusCommand(Guid BoardItemId, string Status) : IRequest<Result>;

public class UpdateBoardItemStatusCommandHandler : IRequestHandler<UpdateBoardItemStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public UpdateBoardItemStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(UpdateBoardItemStatusCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .Include(c => c.Group)
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);
        await _permissions.EnsureCanEditBoardAsync(card.Group.BoardId, _currentUser.UserId, ct);
        card.ChangeStatus(Enum.Parse<CardStatus>(request.Status, ignoreCase: true), _currentUser.UserId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
