using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Views;

public sealed class CalendarViewConfig : BoardViewConfig
{
    private CalendarViewConfig(JsonValue data) : base(data) { }

    public static CalendarViewConfig Create(JsonValue data)
    {
        return new CalendarViewConfig(data);
    }
}
