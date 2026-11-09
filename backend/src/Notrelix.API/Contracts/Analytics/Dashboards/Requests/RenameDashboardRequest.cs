namespace Notrelix.API.Contracts.Analytics.Dashboards.Requests;

/// <param name="Name">The new title, 1 to 120 characters after trimming.</param>
/// <param name="ExpectedVersion">The dashboard version the caller read.</param>
public sealed record RenameDashboardRequest(string Name, long ExpectedVersion);
