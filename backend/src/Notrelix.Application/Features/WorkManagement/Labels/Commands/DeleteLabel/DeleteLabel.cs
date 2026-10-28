using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.DeleteLabel;

[IdempotencyOperation("work-management.labels.delete-label.v1")]
public record DeleteLabelCommand(Guid LabelId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    // A label is part of the Board schema: an Editor change, not Board management.
    public PermissionAction Action => PermissionAction.DeleteField;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.label"), LabelId);
}

public class DeleteLabelCommandHandler : IRequestHandler<DeleteLabelCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteLabelCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteLabelCommand request, CancellationToken ct)
    {
        var label = await _context.Labels.FirstOrDefaultAsync(l => l.Id == request.LabelId, ct);
        if (label is null) throw new NotFoundException(nameof(Label), request.LabelId);

        // The links restrict the label's removal on purpose: the use case takes the label off every Item first,
        // in this transaction, and each Item gets its own history fact.
        var links = await _context.BoardItemLabels.Where(l => l.LabelId == request.LabelId).ToListAsync(ct);
        var now = _dateTimeProvider.UtcNow;
        foreach (var link in links)
        {
            link.RecordRemoval(label.Name, _requestContext.UserId, now);
            _context.BoardItemLabels.Remove(link);
        }
        _context.Labels.Remove(label);
        return Result.Success();
    }
}
