using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardViews;

/// <summary>
/// WM-018 for the Calendar's date basis: a view that is written with a <c>calendarDateField</c> naming a field
/// must name an active Date field of the same board (product section 52: the Calendar needs a Date field and
/// never reads dates from text). <c>span</c> and <c>due</c> are the item's own start and due days, which every
/// board has. A stored value the write leaves unchanged is not checked again: a field deleted after the view
/// chose it is read as the default by the view (degrade), so other changes to the view still save.
/// </summary>
public static class BoardViewFieldReferences
{
    private static readonly string[] BuiltInBases = ["span", "due"];

    public static async Task EnsureCalendarDateFieldAsync(
        IWorkManagementDbContext context,
        Guid boardId,
        BoardViewConfig config,
        BoardViewConfig? previous,
        CancellationToken cancellationToken)
    {
        var wanted = CalendarDateField(config);
        if (wanted is null || BuiltInBases.Contains(wanted)) return;
        if (previous is not null && CalendarDateField(previous) == wanted) return;

        if (!Guid.TryParse(wanted, out var fieldId))
            throw Invalid("The Calendar date field must be a Date field of this board.");
        var field = await context.BoardFields
            .AsNoTracking()
            .Where(f => f.Id == fieldId && f.BoardId == boardId && f.DeletedAt == null)
            .Select(f => new { f.Type })
            .FirstOrDefaultAsync(cancellationToken);
        if (field is null)
            throw Invalid("The Calendar date field is not a field of this board.");
        if (field.Type != FieldType.Date)
            throw Invalid("The Calendar date field must be a Date field; the Calendar never reads dates from text.");
    }

    private static string? CalendarDateField(BoardViewConfig config)
    {
        using var document = JsonDocument.Parse(config.Data.Value);
        var root = document.RootElement;
        return root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty(ViewPresentationKeys.CalendarDateField, out var element)
            && element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : null;
    }

    private static Notrelix.Domain.Common.Exceptions.BusinessRuleException Invalid(string message) =>
        new(WorkManagementRuleCodes.WorkManagement_View_PresentationConfigInvalid, message);
}
