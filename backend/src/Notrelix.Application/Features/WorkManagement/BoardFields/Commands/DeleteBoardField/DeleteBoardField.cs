using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.DeleteBoardField;

public record DeleteBoardFieldCommand(Guid BoardId, Guid ColumnId, string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.DeleteField;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardField, ColumnId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"delete-field:{ColumnId}";
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

        var now = _dateTimeProvider.UtcNow;
        column.SoftDelete(_currentUser.UserId, now);
        return Result.Success();
    }
}
