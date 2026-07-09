using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.CreateBoardGroup;

public record CreateBoardGroupCommand(Guid BoardId, string Title, string? Position, string? Color = null) : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
}

public class CreateBoardGroupCommandHandler : IRequestHandler<CreateBoardGroupCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBoardGroupCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateBoardGroupCommand request, CancellationToken ct)
    {
        var board = await _context.Boards
            .FirstOrDefaultAsync(board => board.Id == request.BoardId && !board.IsArchived, ct);
        if (board is null) throw new NotFoundException("Board", request.BoardId);

        var position = request.Position is not null
            ? FractionalIndex.Create(request.Position)
            : FractionalIndex.Initial();

        var color = request.Color is not null ? Color.Create(request.Color) : Color.Create("#808080");
        var list = BoardGroup.Create(_requestContext.RequireAccountId(), board.WorkspaceId, request.BoardId, request.Title, color, position, _requestContext.UserId, _dateTimeProvider.UtcNow);
        _context.BoardGroups.Add(list);
        return Result<Guid>.Success(list.Id);
    }
}
