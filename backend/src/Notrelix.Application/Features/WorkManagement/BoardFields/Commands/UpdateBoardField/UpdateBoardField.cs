using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;

[IdempotencyOperation("work-management.board-fields.update-board-field.v1")]
public record UpdateBoardFieldCommand(Guid BoardId, Guid ColumnId, string? Name, string? Settings, long ExpectedVersion) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-field"), ColumnId);
}

public class UpdateBoardFieldCommandHandler : IRequestHandler<UpdateBoardFieldCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateBoardFieldCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateBoardFieldCommand request, CancellationToken ct)
    {
        var column = await _context.BoardFields
            .FirstOrDefaultAsync(item => item.Id == request.ColumnId && item.BoardId == request.BoardId, ct);
        if (column is null) throw new NotFoundException(nameof(BoardField), request.ColumnId);

        var now = _dateTimeProvider.UtcNow;

        // Ordinary PATCH changes name and settings only; the field type is not
        // patchable (WMREQ030–031). Omitted values are unchanged.
        if (request.Name is not null)
        {
            column.Rename(request.Name, _currentUser.UserId, now);
        }

        if (request.Settings is not null)
        {
            var settings = FieldSettings.Create(JsonValue.Create(request.Settings)!);
            column.UpdateSettings(settings, _currentUser.UserId, now);
            await ComputedFieldGuard.EnsureAsync(_context, column, ct);
        }

        return Result.Success();
    }
}
