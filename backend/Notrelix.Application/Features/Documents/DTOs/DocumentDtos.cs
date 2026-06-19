namespace Notrelix.Application.Features.Documents.DTOs;

public record PageDto(
    Guid Id,
    Guid WorkspaceId,
    Guid? ParentId,
    string Title,
    string Icon,
    string? CoverImage,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record PageBreadcrumbDto(
    Guid Id,
    string Title,
    string Icon
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
    string Icon,
    Guid? ParentId,
    bool HasChildren
);

public record BlockDto(
    Guid Id,
    Guid PageId,
    Guid? ParentId,
    string Type,
    string Content,
    string Properties,
    string Position,
    int Version,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
