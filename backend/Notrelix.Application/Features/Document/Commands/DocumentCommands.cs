using MediatR;
using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Document.Commands;

// ──────── Create Page ────────
public record CreatePageCommand(
    Guid WorkspaceId,
    string Title,
    Guid? ParentId
) : IRequest<Result<Guid>>;

// ──────── Update Page ────────
public record UpdatePageCommand(
    Guid PageId,
    string? Title,
    string? IconType,
    string? IconValue,
    string? CoverUrl
) : IRequest<Result>;

// ──────── Delete Page ────────
public record DeletePageCommand(Guid PageId) : IRequest<Result>;

// ──────── Move Page ────────
public record MovePageCommand(
    Guid PageId,
    Guid? NewParentId,
    double NewPosition
) : IRequest<Result>;

// ──────── Publish Page ────────
public record PublishPageCommand(Guid PageId) : IRequest<Result>;

// ──────── Archive Page ────────
public record ArchivePageCommand(Guid PageId) : IRequest<Result>;

// ──────── Set Page Deadline ────────
public record SetPageDeadlineCommand(Guid PageId, DateTime? Deadline) : IRequest<Result>;

// ──────── Block Commands ────────
public record CreateBlockCommand(
    Guid PageId,
    string Type,
    string Properties,
    double Position,
    Guid? ParentBlockId
) : IRequest<Result<Guid>>;

public record UpdateBlockCommand(
    Guid BlockId,
    string? Type,
    string? Properties
) : IRequest<Result>;

public record DeleteBlockCommand(Guid BlockId) : IRequest<Result>;

public record ReorderBlocksCommand(
    Guid PageId,
    List<ReorderBlockItem> Items
) : IRequest<Result>;

public record ReorderBlockItem(Guid BlockId, double NewPosition, Guid? NewParentBlockId);
