using System.Text.Json;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Common.Idempotency;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValues;

[IdempotencyOperation("work-management.board-items.update-board-item-field-values.v1")]
public record UpdateBoardItemFieldValuesCommand(Guid BoardItemId, Dictionary<Guid, object?> Values, string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, BoardItemId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"bulk-set-field-values:{BoardItemId}:{string.Join(",", Values.Keys.OrderBy(k => k))}";
}

public class UpdateBoardItemFieldValuesCommandHandler : IRequestHandler<UpdateBoardItemFieldValuesCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IResourceReferenceResolver _resourceResolver;

    public UpdateBoardItemFieldValuesCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider timeProvider,
        IResourceReferenceResolver resourceResolver)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
        _resourceResolver = resourceResolver;
    }

    public async Task<Result> Handle(UpdateBoardItemFieldValuesCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId && c.DeletedAt == null, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var now = _timeProvider.UtcNow;

        var columns = await _context.BoardFields.AsNoTracking()
            .Include(f => f.Options)
            .Where(column => column.BoardId == card.BoardId && request.Values.Keys.Contains(column.Id))
            .ToDictionaryAsync(column => column.Id, ct);

        foreach (var (columnId, value) in request.Values)
        {
            if (columnId == card.BoardId)
            {
                card.Rename(ReadString(value) ?? card.Name, _requestContext.UserId, now);
                continue;
            }

            if (!columns.TryGetValue(columnId, out var column))
                return Result.Failure(new ApplicationError("work.board-item.unsupported-field", $"Unsupported field '{columnId}'.", ApplicationErrorType.Validation));

            var semanticField = ResolveSemanticField(column);
            switch (semanticField)
            {
                case "title":
                    card.Rename(ReadString(value) ?? card.Name, _requestContext.UserId, now);
                    break;
                case "status":
                case "priority":
                    {
                        var fv = FieldValue.Create(JsonValue.Create(JsonSerializer.Serialize(NormalizeStoredValue(value))));
                        card.UpdateFieldValue(column, fv, _requestContext.UserId, now);
                        break;
                    }
                case "due_date":
                    {
                        var dt = ReadDateTime(value);
                        card.SetTimeline(
                            card.StartedAt,
                            dt.HasValue ? new DateTimeOffset(dt.Value, TimeSpan.Zero) : null,
                            _requestContext.UserId,
                            now);
                        break;
                    }
                case "linked_page":
                    {
                        var pageId = ReadGuid(value);
                        if (pageId.HasValue)
                        {
                            await EnsurePageCanBeLinkedAsync(pageId.Value, card.WorkspaceId, ct);
                            var link = BoardItemLink.Create(
                                _requestContext.RequireAccountId(),
                                card.WorkspaceId, card.BoardId, card.Id,
                                ResourceRef.Create(ResourceType.Page, pageId.Value, card.WorkspaceId),
                                BoardItemLinkType.Reference,
                                _requestContext.UserId, now);
                            _context.BoardItemLinks.Add(link);
                        }
                        else
                        {
                            var existingLinks = await _context.BoardItemLinks
                                .Where(l => l.SourceItemId == card.Id)
                                .ToListAsync(ct);
                            _context.BoardItemLinks.RemoveRange(existingLinks);
                        }
                        break;
                    }
                case "assignees":
                    await ReplaceMembers(card, ReadGuidList(value), ct, now);
                    break;
                case "text":
                case "number":
                case "checkbox":
                case "date":
                case "timeline":
                case "progress":
                case "select":
                case "multi_select":
                    {
                        var fv = FieldValue.Create(JsonValue.Create(JsonSerializer.Serialize(NormalizeStoredValue(value))));
                        card.UpdateFieldValue(column, fv, _requestContext.UserId, now);
                        break;
                    }
                default:
                    return Result.Failure(new ApplicationError("work.board-item.unsupported-field", $"Unsupported field '{column.Name}'.", ApplicationErrorType.Validation));
            }
        }

        return Result.Success();
    }

    private async Task EnsurePageCanBeLinkedAsync(Guid pageId, Guid boardWorkspaceId, CancellationToken ct)
    {
        var pageWorkspaceId = await _resourceResolver.GetWorkspaceIdAsync(pageId, ResourceTypes.Page, ct);

        if (!pageWorkspaceId.HasValue)
            throw new NotFoundException(nameof(Page), pageId);

        if (pageWorkspaceId.Value != boardWorkspaceId)
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                "CardPageSameWorkspace",
                "BoardItem can only be linked to a page in the same workspace.");
    }

    private async Task ReplaceMembers(BoardItem card, IReadOnlyCollection<Guid> userIds, CancellationToken ct, DateTimeOffset now)
    {
        var existingMembers = await _context.BoardItemMembers
            .Where(member => member.ItemId == card.Id)
            .ToListAsync(ct);

        var requested = userIds.ToHashSet();
        foreach (var member in existingMembers.Where(member => !requested.Contains(member.UserId)))
        {
            _context.BoardItemMembers.Remove(member);
        }

        var existingUserIds = existingMembers.Select(member => member.UserId).ToHashSet();
        foreach (var userId in requested.Where(userId => !existingUserIds.Contains(userId)))
        {
            var member = BoardItemMember.Create(
                _requestContext.RequireAccountId(),
                card.WorkspaceId, card.BoardId, card.Id,
                userId, _requestContext.UserId, now);
            _context.BoardItemMembers.Add(member);
        }
    }

    private static string ResolveSemanticField(BoardField column)
    {
        var normalizedName = Normalize(column.Name);
        var normalizedType = Normalize(column.Type.ToString());

        if (normalizedName is "task" or "title" or "name") return "title";
        if (normalizedName.Contains("due") && normalizedName.Contains("date")) return "due_date";
        if (normalizedName.Contains("linked") && (normalizedName.Contains("doc") || normalizedName.Contains("page"))) return "linked_page";
        if (normalizedName.Contains("assignee") || normalizedName.Contains("owner") || normalizedType is "person" or "people") return "assignees";

        return normalizedType switch
        {
            "linked_page" => "linked_page",
            "person" or "people" => "assignees",
            "text" or "number" or "checkbox" or "date" or "timeline" or "progress" or "select" or "multi_select" or "status" or "priority" => normalizedType,
            _ => normalizedName
        };
    }

    private static string Normalize(string value) =>
        value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");

    private static string? ReadString(object? value) => value?.ToString();

    private static Guid? ReadGuid(object? value)
    {
        if (value is null) return null;
        if (value is Guid guid) return guid;
        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Null) return null;
            if (element.ValueKind == JsonValueKind.String && Guid.TryParse(element.GetString(), out var parsedElementGuid)) return parsedElementGuid;
        }
        return Guid.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    private static IReadOnlyCollection<Guid> ReadGuidList(object? value)
    {
        if (value is null) return [];
        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                return element.EnumerateArray()
                    .Select(item => ReadGuid(item))
                    .Where(guid => guid.HasValue)
                    .Select(guid => guid!.Value)
                    .ToList();
            }

            var elementGuid = ReadGuid(element);
            return elementGuid.HasValue ? [elementGuid.Value] : [];
        }
        if (value is IEnumerable<Guid> guidValues) return guidValues.ToList();
        if (value is IEnumerable<object> objectValues)
        {
            return objectValues
                .Select(ReadGuid)
                .Where(guid => guid.HasValue)
                .Select(guid => guid!.Value)
                .ToList();
        }

        var single = ReadGuid(value);
        return single.HasValue ? [single.Value] : [];
    }

    private static DateTime? ReadDateTime(object? value)
    {
        if (value is null) return null;
        if (value is DateTime dateTime) return dateTime;
        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Null) return null;
            if (element.ValueKind == JsonValueKind.String && DateTime.TryParse(element.GetString(), out var parsedElementDate)) return parsedElementDate;
        }
        return DateTime.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    private static object? NormalizeStoredValue(object? value)
    {
        if (value is null) return null;
        if (value is JsonElement element) return NormalizeJsonElement(element);
        return value;
    }

    private static object? NormalizeJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when element.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Array => element.EnumerateArray().Select(NormalizeJsonElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(property => property.Name, property => NormalizeJsonElement(property.Value)),
            _ => element.ToString()
        };
    }
}
