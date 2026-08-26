using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.DeleteLabel;

[IdempotencyOperation("work-management.labels.delete-label.v1")]
public record DeleteLabelCommand(Guid LabelId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.label"), LabelId);
}

public class DeleteLabelCommandHandler : IRequestHandler<DeleteLabelCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    public DeleteLabelCommandHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteLabelCommand request, CancellationToken ct)
    {
        var label = await _context.Labels.FirstOrDefaultAsync(l => l.Id == request.LabelId, ct);
        if (label is null) throw new NotFoundException(nameof(Label), request.LabelId);
        _context.Labels.Remove(label);
        return Result.Success();
    }
}
