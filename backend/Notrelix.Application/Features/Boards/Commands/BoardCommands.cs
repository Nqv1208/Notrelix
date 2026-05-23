using MediatR;
using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Boards.Commands;

// ──────── Board ────────
public record UpdateBoardCommand(Guid BoardId, string? Title, string? Description, string? Background, string? Visibility) : IRequest<Result>;
public record ArchiveBoardCommand(Guid BoardId) : IRequest<Result>;

// ──────── List ────────
public record CreateListCommand(Guid BoardId, string Title, double? Position) : IRequest<Result<Guid>>;
public record UpdateListCommand(Guid ListId, string Title) : IRequest<Result>;
public record ArchiveListCommand(Guid ListId) : IRequest<Result>;
public record ReorderListsCommand(Guid BoardId, List<ReorderItem> Items) : IRequest<Result>;

// ──────── Card ────────
public record CreateCardCommand(Guid ListId, string Title, double? Position) : IRequest<Result<Guid>>;
public record UpdateCardCommand(Guid CardId, string? Title, string? DescriptionMd, string? Priority, string? Cover) : IRequest<Result>;
public record MoveCardCommand(Guid CardId, Guid TargetListId, double NewPosition) : IRequest<Result>;
public record SetCardDueDateCommand(Guid CardId, DateTime? DueDate, DateTime? StartDate) : IRequest<Result>;
public record UpdateCardStatusCommand(Guid CardId, string Status) : IRequest<Result>;
public record ArchiveCardCommand(Guid CardId) : IRequest<Result>;
public record LinkPageToCardCommand(Guid CardId, Guid PageId) : IRequest<Result>;

// ──────── Card Members ────────
public record AssignCardMemberCommand(Guid CardId, Guid UserId) : IRequest<Result>;
public record UnassignCardMemberCommand(Guid CardId, Guid UserId) : IRequest<Result>;

// ──────── Labels ────────
public record CreateLabelCommand(Guid BoardId, string Color, string? Name) : IRequest<Result<Guid>>;
public record AddLabelToCardCommand(Guid CardId, Guid LabelId) : IRequest<Result>;
public record RemoveLabelFromCardCommand(Guid CardId, Guid LabelId) : IRequest<Result>;

// ──────── Checklist ────────
public record CreateChecklistCommand(Guid CardId, string Title) : IRequest<Result<Guid>>;
public record CreateChecklistItemCommand(Guid ChecklistId, string Title) : IRequest<Result<Guid>>;
public record ToggleChecklistItemCommand(Guid ChecklistItemId) : IRequest<Result>;

// ──────── Card Links ────────
public record CreateCardLinkCommand(Guid SourceCardId, Guid TargetCardId, string LinkType) : IRequest<Result<Guid>>;
public record DeleteCardLinkCommand(Guid CardLinkId) : IRequest<Result>;

// ──────── Common ────────
public record ReorderItem(Guid Id, double NewPosition);

// ══════════════════════════════════════════════════════════════
// Additional commands needed by Minimal API endpoints
// ══════════════════════════════════════════════════════════════

// ── Board ────────────────────────────────────────────────────
public record UnarchiveBoardCommand(Guid BoardId) : IRequest<Result>;
public record CreateBoardBySlugCommand(string Slug, string Title, string? Description, string? Background, string? Visibility) : IRequest<Result<Guid>>;
public record AddBoardMemberCommand(Guid BoardId, Guid UserId, string? Role) : IRequest<Result>;
public record RemoveBoardMemberCommand(Guid BoardId, Guid UserId) : IRequest<Result>;
public record SaveBoardViewCommand(Guid BoardId, string ViewMode, string? Filters) : IRequest<Result>;

// ── List ─────────────────────────────────────────────────────
public record UnarchiveListCommand(Guid ListId) : IRequest<Result>;

// ── Card ─────────────────────────────────────────────────────
public record UnlinkPageFromCardCommand(Guid CardId) : IRequest<Result>;

// ── Label ────────────────────────────────────────────────────
public record UpdateLabelCommand(Guid LabelId, string? Name, string? Color) : IRequest<Result>;
public record DeleteLabelCommand(Guid LabelId) : IRequest<Result>;

// ── Checklist ────────────────────────────────────────────────
public record UpdateChecklistCommand(Guid ChecklistId, string? Title, double? Position) : IRequest<Result>;
public record DeleteChecklistCommand(Guid ChecklistId) : IRequest<Result>;
public record UpdateChecklistItemCommand(Guid ItemId, string? Title, bool? IsChecked, DateTime? DueDate, Guid? AssigneeId) : IRequest<Result>;
public record DeleteChecklistItemCommand(Guid ItemId) : IRequest<Result>;
