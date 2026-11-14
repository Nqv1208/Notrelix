namespace Notrelix.API.Contracts.Analytics.Dashboards.Requests;

/// <summary>The widget's new grid position and size, with the dashboard version the caller read.</summary>
public sealed record MoveDashboardWidgetRequest(int X, int Y, int W, int H, long ExpectedVersion);
