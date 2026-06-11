namespace Notrelix.Application.Features.Document.DTOs;

public record PageDto(
    Guid Id,
    Guid WorkspaceId,
    Guid? ParentId,
    string Title,
    string? IconType,
    string? IconValue,
    string? CoverUrl,
    double Position,
    short Depth,
    bool IsTemplate,
    bool IsArchived,
    DateTime? PublishedAt,
    DateTime? Deadline,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record PageBreadcrumbDto(
    Guid Id,
    string Title,
    string? IconType,
    string? IconValue
);

public record PageHistoryDto(
    Guid Id,
    Guid ActorId,
    string Action,
    string? ResourceTitle,
    DateTime CreatedAt
);

public record PageTreeItemDto(
    Guid Id,
    string Title,
    string? IconType,
    string? IconValue,
    Guid? ParentId,
    double Position,
    short Depth,
    bool HasChildren
);

public record BlockDto(
    Guid Id,
    Guid PageId,
    Guid? ParentBlockId,
    string Type,
    string Properties,
    string PropertiesJson,
    double Position,
    int Version,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
