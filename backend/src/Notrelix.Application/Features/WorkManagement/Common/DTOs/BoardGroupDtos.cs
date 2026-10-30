namespace Notrelix.Application.Features.WorkManagement.Common.DTOs;

public record BoardGroupDto(
    Guid Id,
    string Title,
    string Color,
    string Position,
    bool IsArchived,
    List<BoardItemSummaryDto> BoardItems,
    long Version,
    // The group's active top-level Items. An archived group lists none, so this is how its size is known.
    int ItemCount = 0
);

public record BoardGroupSchemaDto(
    Guid Id,
    string Title,
    string Color,
    string Position,
    bool IsCollapsed,
    long Version);
