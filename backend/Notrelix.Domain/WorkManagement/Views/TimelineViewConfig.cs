using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Views;

public sealed class TimelineViewConfig : BoardViewConfig
{
    private TimelineViewConfig(JsonValue data) : base(data) { }

    public static TimelineViewConfig Create(JsonValue data)
    {
        return new TimelineViewConfig(data);
    }
}
