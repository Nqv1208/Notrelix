using System.Text.Json;

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

/// <summary>A dashboard with its widgets, as the owner opens it.</summary>
public sealed record DashboardDetailDto(
    Guid Id,
    string Name,
    string Visibility,
    string Status,
    Guid? OwnerUserId,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    List<DashboardWidgetDto> Widgets);

/// <summary>A widget placed on a dashboard: its type, typed configuration and grid position.</summary>
public sealed record DashboardWidgetDto(
    Guid Id,
    string Title,
    string Type,
    JsonElement Config,
    int X,
    int Y,
    int W,
    int H);
