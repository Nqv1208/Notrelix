using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Domain.WorkManagement.Views;

public sealed class KanbanViewConfig : BoardViewConfig
{
    public Guid ColumnFieldId { get; }
    public Guid? SwimlaneFieldId { get; }
    public IReadOnlyCollection<Guid> VisibleFieldIds { get; }

    private KanbanViewConfig(Guid columnFieldId, Guid? swimlaneFieldId, Guid[] visibleFieldIds) 
        : base(JsonValue.EmptyObject())
    {
        ColumnFieldId = columnFieldId;
        SwimlaneFieldId = swimlaneFieldId;
        VisibleFieldIds = visibleFieldIds;
    }

    public static KanbanViewConfig Create(
        BoardField columnField,
        IEnumerable<Guid> visibleFieldIds,
        Guid? swimlaneFieldId = null)
    {
        Guard.NotNull(columnField);
        Guard.NotEmpty(columnField.Id);

        if (!columnField.CanBeUsedAsKanbanColumn())
            throw new BusinessRuleException("Invalid Kanban column field.");

        var ids = visibleFieldIds.ToArray();
        if (ids.Any(id => id == Guid.Empty))
            throw new BusinessRuleException("Visible field IDs cannot be empty.");

        if (swimlaneFieldId.HasValue && swimlaneFieldId.Value == Guid.Empty)
            throw new BusinessRuleException("Swimlane field ID cannot be empty.");

        var deduplicated = ids.Distinct().ToArray();

        return new KanbanViewConfig(columnField.Id, swimlaneFieldId, deduplicated);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ColumnFieldId;
        yield return SwimlaneFieldId;
        foreach (var id in VisibleFieldIds)
            yield return id;
    }
}
