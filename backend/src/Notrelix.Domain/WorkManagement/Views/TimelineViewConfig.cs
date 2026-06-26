namespace Notrelix.Domain.WorkManagement.Views;

public sealed class TimelineViewConfig : BoardViewConfig
{
    private TimelineViewConfig(JsonValue data) : base(data) { }

    public static new TimelineViewConfig Create(JsonValue data)
    {
        return new TimelineViewConfig(data);
    }
}
