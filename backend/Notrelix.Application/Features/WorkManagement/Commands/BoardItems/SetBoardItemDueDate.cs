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

public record SetBoardItemDueDateCommand(Guid BoardItemId, DateTime? DueDate, DateTime? StartDate) : IRequest<Result>;

public class SetBoardItemDueDateCommandHandler : IRequestHandler<SetBoardItemDueDateCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public SetBoardItemDueDateCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(SetBoardItemDueDateCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .Include(c => c.Group)
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);
        await _permissions.EnsureCanEditBoardAsync(card.Group.BoardId, _currentUser.UserId, ct);
        card.SetDueDate(request.DueDate, _currentUser.UserId);
        card.SetStartDate(request.StartDate);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
