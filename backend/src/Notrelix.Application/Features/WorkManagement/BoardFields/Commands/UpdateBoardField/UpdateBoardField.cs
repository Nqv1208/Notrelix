using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;

public record UpdateBoardFieldCommand(Guid BoardId, Guid ColumnId, string? Name, string? FieldType, string? Settings) : ICommand<Result>, ITransactionalRequest;

public class UpdateBoardFieldCommandHandler : IRequestHandler<UpdateBoardFieldCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateBoardFieldCommandHandler(
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

    public async Task<Result> Handle(UpdateBoardFieldCommand request, CancellationToken ct)
    {
        await _permissions.EnsureCanEditBoardAsync(request.BoardId, _currentUser.UserId, ct);

        var column = await _context.BoardFields
            .FirstOrDefaultAsync(item => item.Id == request.ColumnId && item.BoardId == request.BoardId, ct);
        if (column is null) throw new NotFoundException(nameof(BoardField), request.ColumnId);

        var now = _dateTimeProvider.UtcNow;

        if (request.Settings is not null)
        {
            var settings = FieldSettings.Create(JsonValue.Create(request.Settings)!);
            column.UpdateSettings(settings, _currentUser.UserId, now);
        }

        return Result.Success();
    }
}
