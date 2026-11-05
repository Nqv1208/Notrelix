using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.MoveBoardField;

/// <summary>
/// Normal field drag (WM-V3-APP-FIELD-001): moves one field relative to its
/// neighbors under the board's field ordering lock, rewriting only its own key.
/// Replaces the full-list ReorderBoardFields for drag.
/// </summary>
[IdempotencyOperation("work-management.board-fields.move-board-field.v1")]
public record MoveBoardFieldCommand(Guid BoardId, Guid FieldId, long ExpectedVersion, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-field"), FieldId);
}

public class MoveBoardFieldCommandHandler : IRequestHandler<MoveBoardFieldCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly WorkPlacementResolver _placement;
    private readonly IBoardRealtimeNotifier? _realtime;
    private readonly IActorLookupService? _actors;
    private readonly IWorkspaceAccessResolver? _access;

    public MoveBoardFieldCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        WorkPlacementResolver placement,
        IBoardRealtimeNotifier? realtime = null,
        IActorLookupService? actors = null,
        IWorkspaceAccessResolver? access = null)
    {
        _actors = actors;
        _access = access;
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _placement = placement;
    }

    public async Task<Result> Handle(MoveBoardFieldCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(MoveBoardFieldCommand request, CancellationToken ct)
    {
        var field = await _context.BoardFields
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == request.BoardId, ct)
            ?? throw new NotFoundException(nameof(BoardField), request.FieldId);

        FractionalIndex position;
        try
        {
            position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.BoardFields, field.BoardId),
            new WorkPlacementIntent(request.PreviousSiblingId, request.NextSiblingId),
            token => BoardFieldSiblings.LoadAsync(_context, field.BoardId, token),
            ct,
            movedEntityId: field.Id);
        }
        catch (ConflictException stale) when (stale.ErrorCode == WorkOrderingErrorCodes.StalePlacement)
        {
            // The last writer of any other field on the board is whoever moved the neighbours away.
            var lastWriter = await _context.BoardFields
                .Where(other => other.BoardId == field.BoardId && other.Id != field.Id)
                .OrderByDescending(other => other.UpdatedAt)
                .Select(other => other.UpdatedBy)
                .FirstOrDefaultAsync(ct);
            throw await StalePlacementNotice.NameAsync(stale, lastWriter, _requestContext, _actors, _access, ct);
        }

        field.UpdatePosition(position, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
