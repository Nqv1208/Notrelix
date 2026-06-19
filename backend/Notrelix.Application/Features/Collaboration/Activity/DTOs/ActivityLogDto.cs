namespace Notrelix.Application.Features.Collaboration.Activity.DTOs;

public record ActivityLogDto(
    Guid Id,
    Guid ActorId,
    string? ActorName,
    string Action,
    string ResourceType,
    Guid ResourceId,
    string? ResourceTitle,
    DateTime CreatedAt
);
