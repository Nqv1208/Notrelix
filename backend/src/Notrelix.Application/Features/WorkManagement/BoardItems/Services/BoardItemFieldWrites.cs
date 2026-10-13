using System.Globalization;
using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>
/// The one way every write path (status, single value, bulk values, clear) writes
/// an Item's field value:
/// <list type="bullet">
/// <item>the system Due Date field writes the Item's <c>DueAt</c> (the authoritative
/// due date) and stores no field value;</item>
/// <item>any other field goes through <see cref="BoardItem.UpdateFieldValue"/>, which
/// rejects raw values of the other system fields;</item>
/// <item>a write to the Board's Status field (resolved like the read path, see
/// <see cref="WorkItemSemanticFields.ResolveStatusField"/>) keeps the Item's
/// completion in step with the chosen option's category.</item>
/// </list>
/// Changing the category of a Status option keeps completion in step the same
/// way for every active Item holding that option
/// (<see cref="SyncCompletionForOptionAsync"/>).
/// </summary>
public static class BoardItemFieldWrites
{
    /// <summary>The Board's Status field, with its options, resolved like the read path.</summary>
    public static async Task<BoardField?> LoadStatusFieldAsync(IWorkManagementDbContext context, Guid boardId, CancellationToken cancellationToken)
    {
        var statusFields = await context.BoardFields
            .Include(field => field.Options)
            .Where(field => field.BoardId == boardId && field.Type == FieldType.Status && field.DeletedAt == null)
            .ToListAsync(cancellationToken);
        return WorkItemSemanticFields.ResolveStatusField(statusFields);
    }

    public static void Write(BoardItem item, BoardField field, FieldValue value, BoardField? statusField, Guid actorUserId, DateTimeOffset now)
    {
        if (field.IsSystem && field.Type == FieldType.Date)
        {
            FieldValueValidator.Validate(value, FieldType.Date, field.Settings);
            item.SetTimeline(item.StartedAt, ReadDate(value), actorUserId, now);
            return;
        }

        item.UpdateFieldValue(field, value, actorUserId, now);

        if (statusField is not null && field.Id == statusField.Id)
        {
            var optionId = WorkItemSemanticFields.ReadOptionId(value);
            var category = statusField.Options.FirstOrDefault(option => option.Id == optionId)?.Category;
            item.SyncCompletionWithStatus(category, actorUserId, now);
        }
    }

    /// <summary>
    /// After the category of <paramref name="optionId"/> on <paramref name="field"/>
    /// changed: when <paramref name="field"/> is the Board's Status field, every
    /// active (not deleted, not archived) Item whose status is that option is
    /// completed or reopened by the option's new category, exactly as writing that
    /// status would. Runs in the caller's transaction.
    /// </summary>
    public static async Task SyncCompletionForOptionAsync(
        IWorkManagementDbContext context, BoardField field, Guid optionId, Guid actorUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var statusField = await LoadStatusFieldAsync(context, field.BoardId, cancellationToken);
        if (statusField is null || statusField.Id != field.Id)
        {
            return;
        }

        var category = field.Options.FirstOrDefault(option => option.Id == optionId)?.Category;
        var values = await context.BoardItemValues
            .AsNoTracking()
            .Where(value => value.FieldId == field.Id)
            .Select(value => new { value.ItemId, value.Value })
            .ToListAsync(cancellationToken);
        var itemIds = values
            .Where(value => WorkItemSemanticFields.ReadOptionId(value.Value) == optionId)
            .Select(value => value.ItemId)
            .Distinct()
            .ToList();
        if (itemIds.Count == 0)
        {
            return;
        }

        var items = await context.BoardItems
            .Where(item => itemIds.Contains(item.Id) && item.BoardId == field.BoardId && item.DeletedAt == null && !item.IsArchived)
            .ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            item.SyncCompletionWithStatus(category, actorUserId, now);
        }
    }

    private static DateTimeOffset? ReadDate(FieldValue value)
    {
        if (value.IsEmpty) return null;
        using var document = JsonDocument.Parse(value.Data.Value);
        // Stored as an instant: PostgreSQL timestamptz accepts UTC offsets only.
        return DateTimeOffset.ParseExact(document.RootElement.GetString()!, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            .ToUniversalTime();
    }
}
