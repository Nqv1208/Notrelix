using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.Commands.Common;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record ReorderBoardFieldsCommand(Guid BoardId, List<ReorderItem> Items) : IRequest<Result>;

public class ReorderBoardFieldsCommandHandler : IRequestHandler<ReorderBoardFieldsCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public ReorderBoardFieldsCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(ReorderBoardFieldsCommand request, CancellationToken ct)
    {
        await _permissions.EnsureCanEditBoardAsync(request.BoardId, _currentUser.UserId, ct);

        foreach (var item in request.Items)
        {
            var column = await _context.BoardFields
                .FirstOrDefaultAsync(value => value.Id == item.Id && value.BoardId == request.BoardId, ct);
            column?.UpdatePosition(item.NewPosition);
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
