using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Views;

public sealed class TableViewConfig : BoardViewConfig
{
    private TableViewConfig(JsonValue data) : base(data) { }

    public static TableViewConfig Create(JsonValue data)
    {
        return new TableViewConfig(data);
    }
}
