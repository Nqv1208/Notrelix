using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Labels.Commands.AddLabelToBoardItem;

[IdempotencyOperation("work-management.labels.add-label-to-board-item.v1")]
public record AddLabelToBoardItemCommand(Guid BoardItemId, Guid LabelId) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, BoardItemId);
}

public class AddLabelToBoardItemCommandHandler : IRequestHandler<AddLabelToBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddLabelToBoardItemCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(AddLabelToBoardItemCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var label = await _context.Labels
            .FirstOrDefaultAsync(l => l.Id == request.LabelId, ct);
        if (label is null) throw new NotFoundException(nameof(Label), request.LabelId);

        var exists = await _context.BoardItemLabels
            .AnyAsync(l => l.ItemId == request.BoardItemId && l.LabelId == request.LabelId, ct);
        if (exists) return Result.Success();

        var link = BoardItemLabel.Create(
            _requestContext.RequireAccountId(),
            card.WorkspaceId, label.BoardId, request.BoardItemId, request.LabelId,
            _requestContext.UserId, _dateTimeProvider.UtcNow);
        _context.BoardItemLabels.Add(link);
        return Result.Success();
    }
}
