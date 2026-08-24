using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Relations.Commands.DeleteBoardRelation;

[IdempotencyOperation("work-management.relations.delete-board-relation.v1")]
public record DeleteBoardRelationCommand(Guid RelationId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-relation"), RelationId);
}

public class DeleteBoardRelationCommandHandler : IRequestHandler<DeleteBoardRelationCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteBoardRelationCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteBoardRelationCommand request, CancellationToken ct)
    {
        var relation = await _context.BoardRelations
            .FirstOrDefaultAsync(r => r.Id == request.RelationId, ct);
        if (relation is null) throw new NotFoundException(nameof(BoardRelation), request.RelationId);

        relation.Delete(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
