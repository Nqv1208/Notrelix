namespace Notrelix.Application.Features.Documents.DTOs;

public record PageDto(
    Guid Id,
    Guid WorkspaceId,
    Guid? ParentId,
    string Title,
    string Icon,
    string? CoverImage,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    // The Page aggregate version; a versioned write (Documents.Pages.MoveToSpace)
    // sends it back as ExpectedVersion and a stale value is rejected (412).
    long Version,
    // The Workspaces Space the page is placed in; null at the Workspace root
    // and also when the caller cannot see that Space (never disclosed, GOV-029).
    Guid? SpaceId = null
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

/// <summary>A page that mentions another page (derived, see IPageBacklinkReader).</summary>
public record PageBacklinkDto(
    Guid Id,
    string Title,
    string Icon,
    // The Space the mentioning page is placed in; null at the Workspace root
    // and when the caller cannot see that Space (never disclosed, GOV-029).
    Guid? SpaceId = null
);

public record PageBacklinksDto(List<PageBacklinkDto> Docs);

public record PageTreeItemDto(
    Guid Id,
    string Title,
    string Icon,
    Guid? ParentId,
    bool HasChildren,
    // As PageDto.Version.
    long Version,
    // As PageDto.SpaceId: null at the root or when the Space is not visible.
    Guid? SpaceId = null
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
