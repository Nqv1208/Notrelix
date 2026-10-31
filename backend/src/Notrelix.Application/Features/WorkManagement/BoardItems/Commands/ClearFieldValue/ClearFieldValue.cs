using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ClearFieldValue;

[IdempotencyOperation("work-management.board-items.clear-field-value.v1")]
public record ClearFieldValueCommand(Guid ItemId, Guid FieldId, long ExpectedVersion) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ItemId);
}

public class ClearFieldValueCommandHandler : IRequestHandler<ClearFieldValueCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public ClearFieldValueCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider timeProvider,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(ClearFieldValueCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(ClearFieldValueCommand request, CancellationToken ct)
    {
        var item = await _context.BoardItems
            // The value collection and the field options are loaded: an existing value
            // is updated in place (unique per item/field) and option-backed values are
            // validated against the persisted options (SPEC WM-V3-GAP-032).
            .Include(i => i.FieldValues)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, ct);
        if (item is null) throw new NotFoundException(nameof(BoardItem), request.ItemId);

        var field = await _context.BoardFields
            .Include(f => f.Options)
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == item.BoardId, ct);
        if (field is null) throw new NotFoundException(nameof(BoardField), request.FieldId);

        var statusField = await BoardItemFieldWrites.LoadStatusFieldAsync(_context, item.BoardId, ct);
        BoardItemFieldWrites.Write(item, field, FieldValue.Empty(), statusField, _currentUser.UserId, _timeProvider.UtcNow);
        return Result.Success();
    }
}
