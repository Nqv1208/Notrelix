using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.BoardColumns;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.CardMembers;
using global::Notrelix.Application.Features.Boards.Commands.Cards.UpdateCard;
using global::Notrelix.Application.Features.Boards.Commands.Cards.UpdateCardFieldValues;
using global::Notrelix.Application.Features.Boards.Commands.Cards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.Cards.UpdateCardFieldValues;

public record UpdateCardFieldValuesCommand(Guid CardId, Dictionary<Guid, object?> Values) : IRequest<Result>;

public class UpdateCardFieldValuesCommandHandler : IRequestHandler<UpdateCardFieldValuesCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public UpdateCardFieldValuesCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(UpdateCardFieldValuesCommand request, CancellationToken ct)
    {
        var card = await _context.Cards
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.CardId && !c.IsDeleted, ct);
        if (card is null) throw new NotFoundException(nameof(Card), request.CardId);

        var boardInfo = await _context.BoardLists.AsNoTracking()
            .Where(list => list.Id == card.ListId)
            .Select(list => new { list.BoardId, list.Board.WorkspaceId })
            .FirstOrDefaultAsync(ct);
        if (boardInfo is null) throw new NotFoundException(nameof(BoardList), card.ListId);

        await _permissions.EnsureCanEditBoardAsync(boardInfo.BoardId, _currentUser.UserId, ct);

        var columns = await _context.BoardColumns.AsNoTracking()
            .Where(column => column.BoardId == boardInfo.BoardId && request.Values.Keys.Contains(column.Id))
            .ToDictionaryAsync(column => column.Id, ct);

        foreach (var (columnId, value) in request.Values)
        {
            if (columnId == boardInfo.BoardId)
            {
                card.Rename(ReadString(value) ?? card.Title, _currentUser.UserId);
                continue;
            }

            if (!columns.TryGetValue(columnId, out var column))
                return Result.Failure($"Unsupported field '{columnId}'.");

            var semanticField = ResolveSemanticField(column);
            switch (semanticField)
            {
                case "title":
                    card.Rename(ReadString(value) ?? card.Title, _currentUser.UserId);
                    break;
                case "status":
                    card.ChangeStatus(ParseEnum<CardStatus>(value, column.Name), _currentUser.UserId);
                    break;
                case "priority":
                    card.ChangePriority(ParseEnum<CardPriority>(value, column.Name), _currentUser.UserId);
                    break;
                case "due_date":
                    card.SetDueDate(ReadDateTime(value), _currentUser.UserId);
                    break;
                case "linked_page":
                    var pageId = ReadGuid(value);
                    if (pageId.HasValue)
                    {
                        await EnsurePageCanBeLinkedAsync(pageId.Value, boardInfo.WorkspaceId, ct);
                        card.LinkPage(pageId.Value, _currentUser.UserId);
                    }
                    else
                    {
                        card.UnlinkPage(_currentUser.UserId);
                    }
                    break;
                case "assignees":
                    await ReplaceMembers(card, ReadGuidList(value), ct);
                    break;
                case "text":
                case "number":
                case "checkbox":
                case "date":
                case "timeline":
                case "progress":
                case "select":
                case "multi_select":
                    card.UpdateFieldValue(columnId.ToString(), NormalizeStoredValue(value));
                    break;
                default:
                    return Result.Failure($"Unsupported field '{column.Name}'.");
            }
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task EnsurePageCanBeLinkedAsync(Guid pageId, Guid boardWorkspaceId, CancellationToken ct)
    {
        var pageWorkspaceId = await _context.Pages
            .AsNoTracking()
            .Where(page => page.Id == pageId && !page.IsDeleted)
            .Select(page => page.WorkspaceId)
            .FirstOrDefaultAsync(ct);

        if (pageWorkspaceId == Guid.Empty)
            throw new NotFoundException(nameof(Page), pageId);

        if (pageWorkspaceId != boardWorkspaceId)
            throw new BusinessRuleViolationException(
                "CardPageSameWorkspace",
                "Card can only be linked to a page in the same workspace.");
    }

    private async Task ReplaceMembers(Card card, IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        var existingMembers = await _context.CardMembers
            .Where(member => member.CardId == card.Id)
            .ToListAsync(ct);

        var requested = userIds.ToHashSet();
        foreach (var member in existingMembers.Where(member => !requested.Contains(member.UserId)))
        {
            _context.CardMembers.Remove(member);
        }

        var existingUserIds = existingMembers.Select(member => member.UserId).ToHashSet();
        foreach (var userId in requested.Where(userId => !existingUserIds.Contains(userId)))
        {
            card.AssignMember(userId, _currentUser.UserId);
        }
    }

    private static string ResolveSemanticField(BoardColumn column)
    {
        var normalizedName = Normalize(column.Name);
        var normalizedType = Normalize(column.FieldType);

        if (normalizedName is "task" or "title" or "name") return "title";
        if (normalizedName.Contains("status")) return "status";
        if (normalizedName.Contains("priority")) return "priority";
        if (normalizedName.Contains("due") && normalizedName.Contains("date")) return "due_date";
        if (normalizedName.Contains("linked") && (normalizedName.Contains("doc") || normalizedName.Contains("page"))) return "linked_page";
        if (normalizedName.Contains("assignee") || normalizedName.Contains("owner") || normalizedType is "person" or "people") return "assignees";

        return normalizedType switch
        {
            "linked_page" => "linked_page",
            "person" or "people" => "assignees",
            "text" or "number" or "checkbox" or "date" or "timeline" or "progress" or "select" or "multi_select" => normalizedType,
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

    private static TEnum ParseEnum<TEnum>(object? value, string fieldName) where TEnum : struct
    {
        var raw = value?.ToString();
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException($"{fieldName} cannot be empty.");

        var normalizedRaw = raw.Trim().ToLowerInvariant().Replace("_", "-", StringComparison.Ordinal).Replace(" ", "-", StringComparison.Ordinal);
        if (typeof(TEnum) == typeof(CardStatus))
        {
            normalizedRaw = normalizedRaw switch
            {
                "status-working" or "working" or "in-progress" or "inprogress" => "InProgress",
                "status-done" or "done" => "Done",
                "status-completed" or "completed" => "Done",
                "status-stuck" or "stuck" or "blocked" => "Cancelled",
                "status-not-started" or "not-started" or "open" => "Open",
                "in-review" or "inreview" => "InReview",
                _ => raw
            };
        }

        var normalized = normalizedRaw.Replace("_", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
        if (Enum.TryParse<TEnum>(normalized, ignoreCase: true, out var parsed)) return parsed;

        throw new ArgumentException($"{fieldName} value '{raw}' is not supported.");
    }
}
