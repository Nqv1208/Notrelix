namespace Notrelix.API.Contracts.Analytics.Dashboards.Requests;

/// <summary>Body of dashboard archive and restore: the dashboard version the caller read.</summary>
public sealed record DashboardVersionRequest(long ExpectedVersion);
