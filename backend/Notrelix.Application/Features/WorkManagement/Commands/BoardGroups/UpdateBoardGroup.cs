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

public record UpdateBoardGroupCommand(Guid GroupId, string? Title, string? Color = null) : IRequest<Result>;

public class UpdateBoardGroupCommandHandler : IRequestHandler<UpdateBoardGroupCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public UpdateBoardGroupCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(UpdateBoardGroupCommand request, CancellationToken ct)
    {
        var list = await _context.BoardGroups.FirstOrDefaultAsync(l => l.Id == request.GroupId, ct);
        if (list is null) throw new NotFoundException(nameof(BoardGroup), request.GroupId);

        await _permissions.EnsureCanEditBoardAsync(list.BoardId, _currentUser.UserId, ct);

        if (request.Title is not null) list.Rename(request.Title, _currentUser.UserId);
        if (request.Color is not null) list.ChangeColor(request.Color, _currentUser.UserId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
