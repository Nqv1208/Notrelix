using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Relations.Commands.ResumeBoardRelation;

public record ResumeBoardRelationCommand(Guid RelationId, string? IdempotencyKey = null)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardRelation, RelationId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"resume-board-relation:{RelationId}";
}

public class ResumeBoardRelationCommandHandler : IRequestHandler<ResumeBoardRelationCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ResumeBoardRelationCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ResumeBoardRelationCommand request, CancellationToken ct)
    {
        var relation = await _context.BoardRelations
            .FirstOrDefaultAsync(r => r.Id == request.RelationId, ct);
        if (relation is null) throw new NotFoundException(nameof(BoardRelation), request.RelationId);

        relation.Resume(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
