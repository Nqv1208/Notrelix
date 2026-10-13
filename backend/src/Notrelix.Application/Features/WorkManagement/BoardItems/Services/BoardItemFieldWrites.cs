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

    private static DateTimeOffset? ReadDate(FieldValue value)
    {
        if (value.IsEmpty) return null;
        using var document = JsonDocument.Parse(value.Data.Value);
        // Stored as an instant: PostgreSQL timestamptz accepts UTC offsets only.
        return DateTimeOffset.ParseExact(document.RootElement.GetString()!, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            .ToUniversalTime();
    }
}
