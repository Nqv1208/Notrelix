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

public record CreateBoardFieldCommand(Guid BoardId, string Name, string FieldType, string? Settings, double? Position) : IRequest<Result<Guid>>;

public class CreateBoardFieldCommandHandler : IRequestHandler<CreateBoardFieldCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public CreateBoardFieldCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result<Guid>> Handle(CreateBoardFieldCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId && !b.IsArchived, ct);
        if (board is null) throw new NotFoundException(nameof(Board), request.BoardId);

        await _permissions.EnsureCanEditBoardAsync(request.BoardId, _currentUser.UserId, ct);

        var position = request.Position ?? await _context.BoardFields
            .Where(column => column.BoardId == request.BoardId)
            .MaxAsync(column => (double?)column.Position, ct) + 1 ?? 0;

        var settings = FieldSettings.FromJson(request.Settings);

        var type = Enum.TryParse<FieldType>(request.FieldType, true, out var parsedType)
            ? parsedType
            : FieldType.Text;

        var column = BoardField.Create(
            board.WorkspaceId,
            request.BoardId,
            request.Name,
            type,
            settings,
            position);

        _context.BoardFields.Add(column);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(column.Id);
    }
}
