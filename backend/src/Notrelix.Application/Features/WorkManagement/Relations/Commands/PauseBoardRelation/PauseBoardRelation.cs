using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Relations.Commands.PauseBoardRelation;

[IdempotencyOperation("work-management.relations.pause-board-relation.v1")]
public record PauseBoardRelationCommand(Guid RelationId)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardRelation, RelationId);
}

public class PauseBoardRelationCommandHandler : IRequestHandler<PauseBoardRelationCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PauseBoardRelationCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(PauseBoardRelationCommand request, CancellationToken ct)
    {
        var relation = await _context.BoardRelations
            .FirstOrDefaultAsync(r => r.Id == request.RelationId, ct);
        if (relation is null) throw new NotFoundException(nameof(BoardRelation), request.RelationId);

        relation.Pause(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
