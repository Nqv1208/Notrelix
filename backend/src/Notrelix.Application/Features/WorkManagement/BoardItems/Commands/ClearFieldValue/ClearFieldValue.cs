using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ClearFieldValue;

public record ClearFieldValueCommand(Guid ItemId, Guid FieldId, string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, ItemId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"clear-field-value:{ItemId}:{FieldId}";
}

public class ClearFieldValueCommandHandler : IRequestHandler<ClearFieldValueCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;

    public ClearFieldValueCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(ClearFieldValueCommand request, CancellationToken ct)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, ct);
        if (item is null) throw new NotFoundException(nameof(BoardItem), request.ItemId);

        var field = await _context.BoardFields
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == item.BoardId, ct);
        if (field is null) throw new NotFoundException(nameof(BoardField), request.FieldId);

        var emptyValue = FieldValue.Create(JsonValue.Create("null"));
        item.UpdateFieldValue(field, emptyValue, _currentUser.UserId, _timeProvider.UtcNow);
        return Result.Success();
    }
}
