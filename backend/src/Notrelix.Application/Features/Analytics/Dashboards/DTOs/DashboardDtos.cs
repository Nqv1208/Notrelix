namespace Notrelix.Application.Features.Analytics.Dashboards.DTOs;

/// <summary>A dashboard in a list: its metadata only, never the widget configuration.</summary>
public sealed record DashboardSummaryDto(
    Guid Id,
    string Name,
    string Visibility,
    string Status,
    Guid? OwnerUserId,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
