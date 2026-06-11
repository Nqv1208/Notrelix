using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Views;

public sealed class KanbanViewConfig : BoardViewConfig
{
    private KanbanViewConfig(JsonValue data) : base(data) { }

    public static KanbanViewConfig Create(JsonValue data)
    {
        return new KanbanViewConfig(data);
    }
}
