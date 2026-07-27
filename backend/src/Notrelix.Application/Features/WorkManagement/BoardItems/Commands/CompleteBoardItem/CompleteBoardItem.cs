using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CompleteBoardItem;

public record CompleteBoardItemCommand(
    Guid BoardItemId,
    DateTimeOffset? CompletedAt,
    string? IdempotencyKey = null)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, BoardItemId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"complete-item:{BoardItemId}";
}

public class CompleteBoardItemCommandHandler : IRequestHandler<CompleteBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CompleteBoardItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(CompleteBoardItemCommand request, CancellationToken ct)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.BoardItemId, ct);
        if (item is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var completedAt = request.CompletedAt ?? _dateTimeProvider.UtcNow;
        item.Complete(completedAt, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
