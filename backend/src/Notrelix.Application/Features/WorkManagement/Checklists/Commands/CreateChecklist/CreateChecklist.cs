using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklist;

[IdempotencyOperation("work-management.checklists.create-checklist.v1")]
public record CreateChecklistCommand(Guid BoardItemId, string Title) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
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
