using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Common.Idempotency;

namespace Notrelix.Application.Features.WorkManagement.Relations.Commands.CreateBoardRelation;

[IdempotencyOperation("work-management.relations.create-board-relation.v1")]
public record CreateBoardRelationCommand(
    Guid SourceBoardId,
    Guid TargetBoardId,
    string RelationType,
    string? IdempotencyKey = null)
    : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, SourceBoardId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"create-board-relation:{SourceBoardId}:{TargetBoardId}";
}

public class CreateBoardRelationCommandHandler : IRequestHandler<CreateBoardRelationCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBoardRelationCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateBoardRelationCommand request, CancellationToken ct)
    {
        var sourceBoard = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.SourceBoardId, ct);
        if (sourceBoard is null) throw new NotFoundException(nameof(Board), request.SourceBoardId);

        var targetBoard = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.TargetBoardId, ct);
        if (targetBoard is null) throw new NotFoundException(nameof(Board), request.TargetBoardId);

        var relationType = Enum.TryParse<BoardRelationType>(request.RelationType, true, out var parsed)
            ? parsed
            : BoardRelationType.ConnectBoards;

        var now = _dateTimeProvider.UtcNow;

        var relation = BoardRelation.Create(
            _requestContext.RequireAccountId(),
            sourceBoard.WorkspaceId,
            request.SourceBoardId,
            request.TargetBoardId,
            null,
            null,
            _requestContext.UserId,
            now,
            relationType);

        _context.BoardRelations.Add(relation);
        return Result<Guid>.Success(relation.Id);
    }
}
