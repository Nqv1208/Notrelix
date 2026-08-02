using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.ReorderBoardFields;

[IdempotencyOperation("work-management.board-fields.reorder-board-fields.v1")]
public record ReorderBoardFieldsCommand(Guid BoardId, List<ReorderItem> Items, string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"reorder-fields:{BoardId}";
}

public class ReorderBoardFieldsCommandHandler : IRequestHandler<ReorderBoardFieldsCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReorderBoardFieldsCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ReorderBoardFieldsCommand request, CancellationToken ct)
    {
        if (request.Items.Count == 0)
            return Result.Success();

        var now = _dateTimeProvider.UtcNow;
        var sorted = request.Items.OrderBy(x => x.NewPosition).ToList();
        var newPositions = FractionalIndexGenerator.GenerateNKeysBetween(null, null, sorted.Count);
        for (var idx = 0; idx < sorted.Count; idx++)
        {
            var item = sorted[idx];
            var column = await _context.BoardFields
                .FirstOrDefaultAsync(value => value.Id == item.Id && value.BoardId == request.BoardId, ct);
            if (column is not null)
            {
                column.UpdatePosition(newPositions[idx], _currentUser.UserId, now);
            }
        }

        return Result.Success();
    }
}
