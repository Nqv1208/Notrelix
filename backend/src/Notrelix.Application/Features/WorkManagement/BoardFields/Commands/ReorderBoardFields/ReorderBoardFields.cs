using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.ReorderBoardFields;

public record ReorderBoardFieldsCommand(Guid BoardId, List<ReorderItem> Items) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
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
        var now = _dateTimeProvider.UtcNow;
        foreach (var item in request.Items)
        {
            var column = await _context.BoardFields
                .FirstOrDefaultAsync(value => value.Id == item.Id && value.BoardId == request.BoardId, ct);
            if (column is not null)
            {
                column.UpdatePosition(FractionalIndex.Create(item.NewPosition.ToString("F0")), _currentUser.UserId, now);
            }
        }

        return Result.Success();
    }
}
