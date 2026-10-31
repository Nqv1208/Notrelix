using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.DeleteBoardField;

[IdempotencyOperation("work-management.board-fields.delete-board-field.v1")]
public record DeleteBoardFieldCommand(Guid BoardId, Guid ColumnId, long ExpectedVersion) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    // A field is a part of the board's shape, so deleting one is Board management (Manager).
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-field"), ColumnId);
}

public class DeleteBoardFieldCommandHandler : IRequestHandler<DeleteBoardFieldCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteBoardFieldCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteBoardFieldCommand request, CancellationToken ct)
    {
        var column = await _context.BoardFields
            .FirstOrDefaultAsync(item => item.Id == request.ColumnId && item.BoardId == request.BoardId, ct);
        if (column is null) throw new NotFoundException(nameof(BoardField), request.ColumnId);

        await ComputedFieldGuard.EnsureNoDependentsAsync(_context, column, ct);

        var now = _dateTimeProvider.UtcNow;
        column.Delete(_currentUser.UserId, now);
        return Result.Success();
    }
}
