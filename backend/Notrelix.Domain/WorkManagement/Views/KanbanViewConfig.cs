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

        if (!columnField.CanBeUsedAsKanbanColumn())
            throw new BusinessRuleException("Invalid Kanban column field.");

        return new KanbanViewConfig(columnField.Id, swimlaneFieldId, visibleFieldIds.ToArray());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ColumnFieldId;
        yield return SwimlaneFieldId;
        foreach (var id in VisibleFieldIds)
            yield return id;
    }
}
