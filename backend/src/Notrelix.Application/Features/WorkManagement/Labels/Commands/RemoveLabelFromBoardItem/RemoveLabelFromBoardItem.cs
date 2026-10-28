using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.RemoveLabelFromBoardItem;

[IdempotencyOperation("work-management.labels.remove-label-from-board-item.v1")]
public record RemoveLabelFromBoardItemCommand(Guid BoardItemId, Guid LabelId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class RemoveLabelFromBoardItemCommandHandler : IRequestHandler<RemoveLabelFromBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemoveLabelFromBoardItemCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RemoveLabelFromBoardItemCommand request, CancellationToken ct)
    {
        var cl = await _context.BoardItemLabels
            .FirstOrDefaultAsync(l => l.ItemId == request.BoardItemId && l.LabelId == request.LabelId, ct);
        if (cl is not null)
        {
            var name = await _context.Labels.Where(l => l.Id == request.LabelId).Select(l => l.Name).FirstOrDefaultAsync(ct) ?? string.Empty;
            cl.RecordRemoval(name, _requestContext.UserId, _dateTimeProvider.UtcNow);
            _context.BoardItemLabels.Remove(cl);
        }
        return Result.Success();
    }
}
