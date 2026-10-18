namespace Notrelix.Application.Features.WorkManagement.Common.DTOs;

/// <summary>
/// A checklist with its items. <see cref="Version"/> is the Checklist aggregate
/// version, which is also the concurrency authority for every item mutation
/// (SPEC WM-V3-DEC-007); items carry no version of their own.
/// </summary>
public record ChecklistDto(
    Guid Id,
    string Title,
    string Position,
    long Version,
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
