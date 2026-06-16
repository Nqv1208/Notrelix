using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.SharedKernel;
using global::Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record CreateBoardFieldCommand(Guid BoardId, string Name, string FieldType, string? Settings, string? Position) : IRequest<Result<Guid>>;

public class CreateBoardFieldCommandHandler : IRequestHandler<CreateBoardFieldCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBoardFieldCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateBoardFieldCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId && !b.IsArchived, ct);
        if (board is null) throw new NotFoundException(nameof(Board), request.BoardId);

        await _permissions.EnsureCanEditBoardAsync(request.BoardId, _currentUser.UserId, ct);

        var now = _dateTimeProvider.UtcNow;
        var position = request.Position is not null
            ? FractionalIndex.Create(request.Position)
            : FractionalIndex.Create("z");

        var settings = request.Settings is not null
            ? FieldSettings.Create(JsonValue.Create(request.Settings)!)
            : FieldSettings.Empty();

        var type = Enum.TryParse<FieldType>(request.FieldType, true, out var parsedType)
            ? parsedType
            : FieldType.Text;

        var column = BoardField.Create(
            board.WorkspaceId,
            request.BoardId,
            request.Name,
            type,
            settings,
            position,
            _currentUser.UserId,
            now);

        _context.BoardFields.Add(column);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(column.Id);
    }
}
