using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardBySlug;

public record CreateBoardBySlugCommand(string Slug, string Title, string? Description, string? Background, BoardVisibility? Visibility, string? IdempotencyKey = null) : ICommand<Result<Guid>>, ITransactionalRequest, IIdempotentRequest
{
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"create-board-slug:{Slug}";
}

public class CreateBoardBySlugCommandHandler : IRequestHandler<CreateBoardBySlugCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspaceAccessResolver _workspaceAccess;

    public CreateBoardBySlugCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IWorkspaceAccessResolver workspaceAccess)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _workspaceAccess = workspaceAccess;
    }

    public async Task<Result<Guid>> Handle(CreateBoardBySlugCommand request, CancellationToken ct)
    {
        var workspace = await _workspaceAccess.ResolveBySlugAsync(request.Slug, ct);

        if (workspace is null) throw new NotFoundException(nameof(Workspace), request.Slug);

        var createdAt = _dateTimeProvider.UtcNow;
        var visibility = request.Visibility ?? BoardVisibility.Workspace;

        var board = BoardEntity.Create(_requestContext.RequireAccountId(), workspace.Id, _requestContext.UserId, request.Title, request.Description, createdAt, visibility);

        if (request.Background is not null) board.UpdateBackground(request.Background, _requestContext.UserId, createdAt);

        _context.Boards.Add(board);

        var defaultFields = new[]
        {
            BoardFieldEntity.Create(_requestContext.RequireAccountId(), board.WorkspaceId, board.Id, "Title", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), _requestContext.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(_requestContext.RequireAccountId(), board.WorkspaceId, board.Id, "Status", FieldType.Status, FieldSettings.Empty(), FractionalIndex.Create("a1"), _requestContext.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(_requestContext.RequireAccountId(), board.WorkspaceId, board.Id, "Assignee", FieldType.Person, FieldSettings.Empty(), FractionalIndex.Create("a2"), _requestContext.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(_requestContext.RequireAccountId(), board.WorkspaceId, board.Id, "Due Date", FieldType.Date, FieldSettings.Empty(), FractionalIndex.Create("a3"), _requestContext.UserId, createdAt, isSystem: true),
        };
        _context.BoardFields.AddRange(defaultFields);

        return Result<Guid>.Success(board.Id);
    }
}
