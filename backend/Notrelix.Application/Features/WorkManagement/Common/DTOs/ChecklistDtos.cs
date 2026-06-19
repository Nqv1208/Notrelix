namespace Notrelix.Application.Features.WorkManagement.Common.DTOs;

public record ChecklistDto(
    Guid Id,
    string Title,
    string Position,
    List<ChecklistItemDto> Items
);

public record ChecklistItemDto(
    Guid Id,
    string Title,
    string Status,
    DateTime? DueAt,
    Guid? AssigneeUserId,
    string Position
);
