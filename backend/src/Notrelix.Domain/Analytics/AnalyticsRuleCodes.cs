namespace Notrelix.Domain.Analytics;

/// <summary>
/// Rule codes for the Analytics bounded context.
/// </summary>
public static class AnalyticsRuleCodes
{
    public const string Analytics_Dashboard_WidgetNotFound = "Analytics_Dashboard_WidgetNotFound";
    public const string Analytics_Dashboard_InvalidWidgetConfig = "Analytics_Dashboard_InvalidWidgetConfig";
    public const string Analytics_Dashboard_WidgetLimitExceeded = "Analytics_Dashboard_WidgetLimitExceeded";

    // ── Widget position/dimensions ─────────────────────────────────────────
    public const string Analytics_Widget_CoordinatesMustBeNonNegative = "Analytics_Widget_CoordinatesMustBeNonNegative";
    public const string Analytics_Widget_DimensionsMustBePositive = "Analytics_Widget_DimensionsMustBePositive";

    // ── Snapshot ──────────────────────────────────────────────────────────
    public const string Analytics_Snapshot_DataMustBeJsonObject = "Analytics_Snapshot_DataMustBeJsonObject";
    public const string Analytics_Snapshot_InvalidDataJson = "Analytics_Snapshot_InvalidDataJson";
    public const string Analytics_Snapshot_CapturedAtDefault = "Analytics_Snapshot_CapturedAtDefault";
}
