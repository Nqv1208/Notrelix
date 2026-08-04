using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;

[IdempotencyOperation("work-management.board-fields.update-board-field.v1")]
public record UpdateBoardFieldCommand(Guid BoardId, Guid ColumnId, string? Name, string? FieldType, string? Settings, long? ExpectedVersion = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-field"), ColumnId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion ?? 0;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
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

        if (request.Settings is not null)
        {
            var settings = FieldSettings.Create(JsonValue.Create(request.Settings)!);
            column.UpdateSettings(settings, _currentUser.UserId, now);
        }

        return Result.Success();
    }
}
