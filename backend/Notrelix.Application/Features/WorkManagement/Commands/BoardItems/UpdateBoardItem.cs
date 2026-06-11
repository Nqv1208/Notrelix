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

public record UpdateBoardItemCommand(Guid BoardItemId, string? Title, string? DescriptionMd, string? Priority, string? Cover, DateTime? DueDate, DateTime? StartDate) : IRequest<Result>;

public class UpdateBoardItemCommandHandler : IRequestHandler<UpdateBoardItemCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public UpdateBoardItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(UpdateBoardItemCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .Include(c => c.Group)
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId && !c.IsDeleted, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        await _permissions.EnsureCanEditBoardAsync(card.Group.BoardId, _currentUser.UserId, ct);

        if (request.Title is not null) card.Rename(request.Title, _currentUser.UserId);
        if (request.DescriptionMd is not null) card.UpdateDescription(request.DescriptionMd);
        if (request.Priority is not null)
            card.ChangePriority(Enum.Parse<CardPriority>(request.Priority, ignoreCase: true), _currentUser.UserId);
        if (request.Cover is not null) card.UpdateCover(request.Cover);
        if (request.DueDate.HasValue || request.StartDate.HasValue) card.SetDueDate(request.DueDate, _currentUser.UserId);
        if (request.StartDate.HasValue) card.SetStartDate(request.StartDate);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
