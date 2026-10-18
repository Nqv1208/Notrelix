namespace Notrelix.Application.Features.WorkManagement.Common.DTOs;

public record BoardGroupDto(
    Guid Id,
    string Title,
    string Color,
    string Position,
    bool IsArchived,
    List<BoardItemSummaryDto> BoardItems,
    long Version
);

public record BoardGroupSchemaDto(
    Guid Id,
    string Title,
    string Color,
    string Position,
    bool IsCollapsed,
    long Version);
