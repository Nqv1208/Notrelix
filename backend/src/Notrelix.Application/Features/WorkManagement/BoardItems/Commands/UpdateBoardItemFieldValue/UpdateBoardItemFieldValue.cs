using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Boards.Services;
using Notrelix.Application.Features.WorkManagement.RelationTargets;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;

[IdempotencyOperation("work-management.board-items.update-board-item-field-value.v1")]
public record UpdateBoardItemFieldValueCommand(
    Guid ItemId,
    Guid FieldId,
    object? Value,
    long ExpectedVersion) : ICommand<BoardItemSlimDto>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ItemId);
}

public class UpdateBoardItemFieldValueCommandHandler : IRequestHandler<UpdateBoardItemFieldValueCommand, BoardItemSlimDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;
    private readonly IActorLookupService? _actors;
    private readonly ICurrentRequestContext? _requestContext;
    private readonly IResourceVisibility? _visibility;

    public UpdateBoardItemFieldValueCommandHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider timeProvider, IBoardRealtimeNotifier? realtime = null, IActorLookupService? actors = null, ICurrentRequestContext? requestContext = null, IResourceVisibility? visibility = null)
    {
        _visibility = visibility;
        _actors = actors;
        _requestContext = requestContext;
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<BoardItemSlimDto> Handle(UpdateBoardItemFieldValueCommand request, CancellationToken cancellationToken)
    {
        var response = await HandleCore(request, cancellationToken);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, cancellationToken);
        return response;
    }

    private async Task<BoardItemSlimDto> HandleCore(UpdateBoardItemFieldValueCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.BoardItems
            // The value collection and the field options are loaded: an existing value
            // is updated in place (unique per item/field) and option-backed values are
            // validated against the persisted options (SPEC WM-V3-GAP-032).
            .Include(i => i.FieldValues)
            .FirstOrDefaultAsync(item => item.Id == request.ItemId, cancellationToken);

        if (item == null)
            throw new NotFoundException("BoardItem", request.ItemId);

        var field = await _context.BoardFields
            .Include(f => f.Options)
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == item.BoardId, cancellationToken);

        if (field == null)
            throw _requestContext is null
                ? new NotFoundException("BoardField", request.FieldId)
                : await BoardFields.StaleFieldGuard.MissingAsync(_context, _actors, _requestContext, item.BoardId, request.FieldId, cancellationToken);

        // A person field names who the work is for. The value is validated first (shape, size, at most 50 people),
        // so only a well-formed set reaches the checks: each person must be able to view the board (share it
        // first) and a board Viewer is not offered there.
        if (field.Type == FieldType.Person && request.Value is not null)
        {
            FieldValueValidator.Validate(FieldValue.Create(JsonValue.Create(System.Text.Json.JsonSerializer.Serialize(request.Value))), field.Type, field.Settings);
            var named = new HashSet<Guid>();
            using (var doc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(request.Value)))
            {
                var root = doc.RootElement;
                if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
                    foreach (var e in root.EnumerateArray()) named.Add(Guid.Parse(e.GetString()!));
                else if (root.ValueKind == System.Text.Json.JsonValueKind.String)
                    named.Add(Guid.Parse(root.GetString()!));
            }
            if (named.Count > 0)
            {
                if (_visibility is not null && _requestContext is not null)
                    foreach (var userId in named)
                        if (!(await _visibility.VisibleBoardIdsAsync(item.WorkspaceId, userId, cancellationToken)).Contains(item.BoardId))
                            throw new ForbiddenException("Người này chưa xem được board của việc, nên chưa thể chọn. Hãy chia sẻ board với họ trước.", BoardAssigneeGuard.BoardNotSharedCode);
                await BoardAssigneeGuard.EnsureNoneViewerAsync(_context, item.BoardId, named, cancellationToken);
            }
        }

        // A Relation field links items of another board: the shape is checked first, then each link must be a live item of
        // the target board in this account and Workspace, and a new link needs that board readable by the writer.
        if (field.Type == FieldType.Relation && request.Value is not null)
        {
            var linkJson = System.Text.Json.JsonSerializer.Serialize(request.Value);
            FieldValueValidator.Validate(FieldValue.Create(JsonValue.Create(linkJson)), field.Type, field.Settings);
            var targetBoardId = RelationFieldTargets.TargetBoardIdOf(field)
                ?? throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Relation_TargetBoardRequired, "A Relation field needs a target board.");
            await RelationFieldTargets.EnsureLinksAsync(
                _context, _visibility, item.AccountId, item.WorkspaceId, _currentUser.UserId, targetBoardId,
                RelationFieldTargets.ParseLinkIds(linkJson), RelationFieldTargets.StoredLinkIds(item, field.Id), cancellationToken);
        }

        var now = _timeProvider.UtcNow;
        var jsonString = System.Text.Json.JsonSerializer.Serialize(request.Value);
        var fieldValue = FieldValue.Create(JsonValue.Create(jsonString));

        BoardFields.StaleFieldGuard.EnsureOptionsExist(field, fieldValue);
        BoardItemFieldWrites.EnsureNotClearingRequired(field, fieldValue);
        var statusField = await BoardItemFieldWrites.LoadStatusFieldAsync(_context, item.BoardId, cancellationToken);
        BoardItemFieldWrites.Write(item, field, fieldValue, statusField, _currentUser.UserId, now);

        var memberIds = await _context.BoardItemMembers
            .Where(m => m.ItemId == item.Id)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var labelIds = await _context.BoardItemLabels
            .Where(l => l.ItemId == item.Id)
            .Select(l => l.LabelId)
            .ToListAsync(cancellationToken);

        var response = new BoardItemSlimDto(
            item.Id,
            item.GroupId,
            item.Name,
            item.Position.Value,
            memberIds,
            labelIds,
            item.Version
        );
        return response;
    }
}
