namespace Notrelix.API.Contracts.Analytics.Dashboards.Requests;

/// <param name="Name">The dashboard title, 1 to 120 characters after trimming.</param>
public sealed record CreateDashboardRequest(string Name);
