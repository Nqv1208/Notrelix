using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklistItem;

[IdempotencyOperation("work-management.checklists.create-checklist-item.v1")]
public record CreateChecklistItemCommand(Guid ChecklistId, string Title) : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.checklist"), ChecklistId);
}

public class CreateChecklistItemCommandHandler : IRequestHandler<CreateChecklistItemCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    public CreateChecklistItemCommandHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateChecklistItemCommand request, CancellationToken ct)
    {
        var position = FractionalIndex.Initial();
        var item = ChecklistItem.Create(request.ChecklistId, request.Title, position);
        _context.ChecklistItems.Add(item);
        return Result<Guid>.Success(item.Id);
    }
}
