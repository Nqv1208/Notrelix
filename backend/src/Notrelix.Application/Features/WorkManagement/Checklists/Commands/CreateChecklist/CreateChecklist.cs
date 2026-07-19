using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklist;

public record CreateChecklistCommand(Guid BoardItemId, string Title, string? IdempotencyKey = null) : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, BoardItemId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"create-checklist:{BoardItemId}";
}

public class CreateChecklistCommandHandler : IRequestHandler<CreateChecklistCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateChecklistCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateChecklistCommand request, CancellationToken ct)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.BoardItemId, ct);

        if (item is null)
            throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var position = FractionalIndex.Initial();
        var checklist = Checklist.Create(_requestContext.RequireAccountId(), item.WorkspaceId, request.BoardItemId, request.Title, position, _requestContext.UserId, _dateTimeProvider.UtcNow);
        _context.Checklists.Add(checklist);
        return Result<Guid>.Success(checklist.Id);
    }
}
