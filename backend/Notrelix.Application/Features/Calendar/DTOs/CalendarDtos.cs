namespace Notrelix.Application.Features.Calendar.DTOs;

public record CalendarIntegrationDto(
    Guid Id,
    string Provider,
    string? ProviderAccountEmail,
    string? CalendarId,
    string SyncDirection,
    DateTime? LastSyncedAt,
    bool IsActive,
    DateTime CreatedAt
);

public record CalendarEventDto(
    Guid Id,
    Guid IntegrationId,
    string ExternalEventId,
    string ResourceType,
    Guid ResourceId,
    DateTime SyncedAt
);
