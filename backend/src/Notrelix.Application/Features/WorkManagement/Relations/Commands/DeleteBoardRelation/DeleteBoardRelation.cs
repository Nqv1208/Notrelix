using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Relations.Commands.DeleteBoardRelation;

public record DeleteBoardRelationCommand(Guid RelationId, string? IdempotencyKey = null)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardRelation, RelationId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"delete-board-relation:{RelationId}";
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

        relation.SoftDelete(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
