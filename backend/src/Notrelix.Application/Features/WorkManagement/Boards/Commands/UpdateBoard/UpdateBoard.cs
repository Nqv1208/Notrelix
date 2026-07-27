using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;

public record UpdateBoardCommand(
    Guid BoardId,
    string? Title,
    string? Description,
    string? Background,
    BoardVisibility? Visibility,
    long? ExpectedVersion,
    string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion ?? 0;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"update-board:{BoardId}";
}

public class UpdateBoardCommandHandler : IRequestHandler<UpdateBoardCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateBoardCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateBoardCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var now = _dateTimeProvider.UtcNow;
        if (request.Title is not null) board.Rename(request.Title, _currentUser.UserId, now);
        if (request.Description is not null) board.UpdateDescription(request.Description, _currentUser.UserId, now);
        if (request.Background is not null) board.UpdateBackground(request.Background, _currentUser.UserId, now);
        if (request.Visibility is not null)
        {
            board.ChangeVisibility(request.Visibility.Value, _currentUser.UserId, now);
        }

        return Result.Success();
    }
}
