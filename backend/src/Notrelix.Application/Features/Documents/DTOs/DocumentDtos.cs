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

/// <summary>
/// A block as the editor reads it. <c>Content</c> is rich text v1 (<c>{"v":1,"runs":[...]}</c>) for a
/// text-like type and an empty object otherwise; <c>Properties</c> holds the type's own settings. Both are
/// JSON objects, not JSON-encoded strings. <c>Version</c> is what a write sends back as <c>expectedVersion</c>.
/// </summary>
public record BlockDto(
    Guid Id,
    Guid PageId,
    Guid? ParentId,
    string Type,
    System.Text.Json.JsonElement Content,
    System.Text.Json.JsonElement Properties,
    string Position,
    int Version,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>
/// One entry of a page's history. <c>Kind</c> is <c>automatic</c>, <c>named</c> or <c>restore</c>; <c>Name</c> is
/// the person's name for a named version and "Restored from version N" for a restore. <c>VersionNumber</c> is the
/// entry's place in this page's history; it is not the page's or a block's concurrency version.
/// </summary>
public record PageVersionDto(
    Guid Id,
    Guid PageId,
    int VersionNumber,
    string Kind,
    string? Name,
    Guid? CreatedBy,
    DateTime CreatedAt
);

/// <summary>A block as a snapshot holds it: no concurrency version, since a snapshot is never edited.</summary>
public record PageVersionBlockDto(
    Guid Id,
    Guid? ParentId,
    string Type,
    System.Text.Json.JsonElement Content,
    System.Text.Json.JsonElement Properties,
    string Position
);

/// <summary>The captured content of one history entry, read-only.</summary>
public record PageVersionDetailDto(
    PageVersionDto Version,
    string Title,
    List<PageVersionBlockDto> Blocks
);

/// <summary>The outcome of a restore: the new history entry and the blocks the page holds now.</summary>
public record RestorePageVersionResultDto(
    PageVersionDto Version,
    List<BlockDto> Blocks
);

/// <summary>A workspace page template (creation input; a later edit of its source page never changes it).</summary>
public record PageTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    string Title,
    string Icon,
    int BlockCount,
    Guid? CreatedBy,
    DateTime CreatedAt
);

/// <summary>A page created from a template; <c>DroppedBlockCount</c> counts the image, file and video blocks that were not copied.</summary>
public record PageFromTemplateDto(
    Guid PageId,
    int DroppedBlockCount
);

/// <summary>The version of one block as the caller last read it (what a restore is checked against).</summary>
public record SeenBlockVersion(Guid BlockId, long Version);
